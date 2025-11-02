global using ParseResult = (QueryByShape.Analyzer.QueryMetadata Metadata, QueryByShape.Analyzer.EquatableArray<QueryByShape.Analyzer.DiagnosticMetadata> Diagnostics);
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using QueryByShape.Analyzer.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Xml.Linq;

namespace QueryByShape.Analyzer
{
    internal class QueryParser(NamedTypeSymbols symbols)
    {
        private static readonly SymbolEqualityComparer _symbolComparer = SymbolEqualityComparer.Default;
        private static readonly StringComparer _stringComparer = StringComparer.Ordinal;

        private readonly List<DiagnosticMetadata> _diagnostics = new();
        private readonly Dictionary<INamedTypeSymbol, (TypeMetadata, List<ArgumentMetadata>)> _typeCache =
            new(_symbolComparer);

        public static EquatableArray<ParseResult> Process(ImmutableArray<(TypeDeclarationSyntax declaration, SemanticModel semanticModel)> contexts, NamedTypeSymbols symbols, CancellationToken cancellationToken)
        {
            var items = new ParseResult[contexts.Length];
            var parser = new QueryParser(symbols);

            for (int i = 0; i < contexts.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (declaration, semanticModel) = contexts[i];
                var declaredSymbol = semanticModel.GetDeclaredSymbol(declaration)!;
                
                items[i] = parser.Parse(declaration, declaredSymbol);
            }

            return [.. items];
        }

        public ParseResult Parse(TypeDeclarationSyntax typeDeclaration, INamedTypeSymbol declaredSymbol)
        {
            if (typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)) is false) 
            {
                //_diagnostics.Add(QueryMustBePartialDiagnostic.Create(declaredSymbol.Name, typeDeclaration.GetLocation()));
            }

            var (types, queryArguments) = ParseTypeMetadata(declaredSymbol);
            
            var query = new QueryMetadata(declaredSymbol.Name, declaredSymbol.GetNamespace(), types);

            UpdateQueryFromAttributes(query, declaredSymbol.GetAttributes());
            ValidateVariables(query.Variables, queryArguments);
            
            return (query, [.. _diagnostics]);
        }

        public void ValidateVariables(EquatableArray<VariableMetadata>? variables, IList<ArgumentMetadata> arguments)
        {
            // Nothing to validate
            if ((variables is null or { Count: 0 }) && arguments is { Count: 0 })
            {
                return;
            }

            var vars = variables?.GetArray() ?? Array.Empty<VariableMetadata>();

            var variableRefs = new ReferenceSet<string, VariableMetadata>(vars, v => v.Name, StringComparer.Ordinal);

            foreach (var argument in arguments)
            {
                var varName = argument.VariableName;

                if (variableRefs.TryMarkReferenced(varName) is false)
                {
                    _diagnostics.Add(MissingVariableDiagnostic.CreateMetadata(argument.Name, varName, argument.Reference.GetLocation()));
                    continue;
                }
            }

            // Any remaining variables were not used by any argument
            foreach (var variable in variableRefs.GetUnreferenced())
            {
                _diagnostics.Add(UnusedVariableDiagnostic.CreateMetadata(variable.Name, variable.Reference.GetLocation()));
            }
        }


        private void UpdateQueryFromAttributes(QueryMetadata query, ImmutableArray<AttributeData> attributes)
        {
            Dictionary<string, VariableMetadata> variables = new(_stringComparer);

            foreach (var attribute in attributes)
            {
                if (attribute.IsAttributeType(symbols.QueryAttribute))
                {
                    foreach (var argument in attribute.NamedArguments)
                    {
                        switch (argument.Key)
                        {
                            case nameof(QueryAttribute.OperationName):
                                query.Name = (string)argument.Value.Value!;
                                break;
                            case nameof(QueryAttribute.IncludeFields) when argument.Value.Value is bool includeFields:
                                query.Options.IncludeFields = includeFields;
                                break;
                            case nameof(QueryAttribute.PropertyNamingPolicy) when argument.Value.Value is JsonPropertyNaming jsonPropertyNaming:
                                query.Options.PropertyNamingPolicy = jsonPropertyNaming;
                                break;
                            case nameof(QueryAttribute.Formatting) when argument.Value.Value is SourceFormatting sourceFormatting:
                                query.Options.Formatting = sourceFormatting;
                                break;
                        }
                    }
                 
                }
                else if (attribute.IsAttributeType(symbols.VariableAttribute))
                { 
                    if (attribute.TryGetConstructorArguments(out string? variableName, out string? graphType) 
                        && variables.ContainsKey(variableName) is false)
                    {
                        var defaultValue = attribute.TryGetNamedArgument<object>(nameof(VariableAttribute.DefaultValue), out var value) ? value : null;
                        variables[variableName] = new VariableMetadata(variableName, graphType, defaultValue, attribute.ApplicationSyntaxReference);
                    }
                }
            }

            query.Variables = [.. variables.Values];
        }

        private (TypeMetadata, List<ArgumentMetadata>) ParseTypeMetadata(INamedTypeSymbol type)
        { 
            if (_typeCache.TryGetValue(type, out var cachedType))
            {
                return cachedType;
            }

            INamedTypeSymbol? current = type;

            Dictionary<string, MemberMetadata?> members = new(_stringComparer);
            
            List<ArgumentMetadata> arguments = [];

            while (current is not null && current.SpecialType is not SpecialType.System_Object and not SpecialType.System_ValueType)
            {
                foreach (var member in current.GetMembers())
                {
                    if ((member.Kind != SymbolKind.Property && member.Kind != SymbolKind.Field) || member.IsImplicitlyDeclared)
                    {
                        continue;
                    }

                    var memberName = member.Name;
                    var attributes = member.GetAttributes();
        
                    if (members.TryGetValue(memberName, out var metadata) is false)
                    {
                        metadata = ParseMemberMetadata(member, out var memberType);
                        members[memberName] = metadata;

                        if (metadata.IsSerializable is false)
                        {
                            continue;
                        }

                        if (symbols.TryGetChildrenType(memberType!, out var childrenType))
                        {
                            var (childMetadata, childArguments) = ParseTypeMetadata(childrenType!);
                            metadata.ChildrenType = childMetadata;
                            arguments.AddRange(childArguments);
                        }

                        UpdateMemberFromBaseAttributes(metadata, attributes);
            
                        if (metadata.Arguments?.Count > 0)
                        {
                            arguments.AddRange(metadata.Arguments);
                        }
                    }

                    if (metadata != null)
                    {
                        UpdateMemberFromInheritedAttributes(metadata, attributes);
                        
                        if (metadata.Ignore is true)
                        {
                            members[memberName] = null;
                        }
                    }
                }

                current = current.BaseType;
            }

            var typeMembers = members.Values.Where(m => m.IsSerializable && m.Ignore is not true).ToArray();
            var typeMetadata = new TypeMetadata(type.ToDisplayString(), [.. typeMembers]);

            var result = (typeMetadata, arguments);
            _typeCache[type] = result;
            return result;
        }

        private bool TryGetMemberType(ISymbol member, [NotNullWhen(true)] out INamedTypeSymbol? memberType)
        {
            memberType = member switch
            {
                IPropertySymbol property when symbols.IsPropertySerializable(property) => property.Type,
                IFieldSymbol field when symbols.IsFieldSerializable(field) => field.Type,
                _ => null
            } as INamedTypeSymbol;
            
            return memberType is not null;
        }


        private MemberMetadata ParseMemberMetadata(ISymbol member, out INamedTypeSymbol? memberType)
        {
            memberType = member switch
            {
                IPropertySymbol property when symbols.IsPropertySerializable(property) => property.Type,
                IFieldSymbol field when symbols.IsFieldSerializable(field) => field.Type,
                _ => null
            } as INamedTypeSymbol;

            return new MemberMetadata(member.Name, member.Kind)
            {
                IsSerializable = memberType != null && symbols.IsTypeSerializable(memberType)
            };
        }

        private void UpdateMemberFromBaseAttributes(MemberMetadata metadata, ImmutableArray<AttributeData> attributes)
        {
            Dictionary<string, ArgumentMetadata> arguments = new(_stringComparer);
            HashSet<string> inlineFragments = new(_stringComparer);

            foreach (var attribute in attributes)
            {
                if (attribute.IsAttributeType(symbols.ArgumentAttribute))
                {
                    if (attribute.TryGetConstructorArguments(out string? name, out string? variableName)
                        && arguments.ContainsKey(name) is false)
                    {
                        var argMetadata = new ArgumentMetadata(name, variableName, attribute.ApplicationSyntaxReference);
                        arguments[name] = argMetadata;
                    }
                }
                else if (attribute.IsAttributeType(symbols.AliasOfAttribute))
                {
                    metadata.AliasOf = attribute.TryGetConstructorArgument(out string? alias) ? alias : null;
                }
                else if (attribute.IsAttributeType(symbols.OnAttribute))
                {
                    if (attribute.TryGetConstructorArgument(out string? on))
                    {
                        inlineFragments.Add(on);
                    }
                }
            }

            metadata.Arguments = [.. arguments.Values];
            metadata.On = [.. inlineFragments];
        }

        private void UpdateMemberFromInheritedAttributes(MemberMetadata metadata, ImmutableArray<AttributeData> attributes)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.IsAttributeType(symbols.JsonPropertyAttribute) && metadata.OverrideName is null)
                {
                    metadata.OverrideName = attribute.TryGetConstructorArgument(out string? overrideName) ? overrideName : null;
                }
                else if (attribute.IsAttributeType(symbols.JsonIgnoreAttribute) && metadata.Ignore is null)
                { 
                    var ignoreCondition = attribute.TryGetNamedArgument<int>(nameof(JsonIgnoreAttribute.Condition), out var condition)
                            ? (JsonIgnoreCondition)condition
                            : JsonIgnoreCondition.Always;

                    metadata.Ignore = ignoreCondition is not JsonIgnoreCondition.Never;       
                }
            }
        }
    }
}