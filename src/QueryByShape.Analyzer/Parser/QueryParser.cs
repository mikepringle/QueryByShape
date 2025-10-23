global using ParseResult = (QueryByShape.Analyzer.QueryMetadata Metadata, QueryByShape.Analyzer.EquatableArray<QueryByShape.Analyzer.DiagnosticMetadata> Diagnostics);
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using QueryByShape.Analyzer.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading;

namespace QueryByShape.Analyzer
{
    internal class QueryParser(NamedTypeSymbols symbols)
    {
        private readonly List<DiagnosticMetadata> _diagnostics = [];
        private readonly Dictionary<INamedTypeSymbol, (TypeMetadata, List<ArgumentMetadata>)> _typeCache = new(SymbolEqualityComparer.Default);

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

        public void ValidateVariables(IEnumerable<VariableMetadata>? variables, IList<ArgumentMetadata> arguments)
        {
            // Nothing to validate
            if (variables?.Any() != true && arguments.Count == 0)
            {
                return;
            }

            var variableLookup = variables?.ToDictionary(v => v.Name, StringComparer.Ordinal) ?? new Dictionary<string, VariableMetadata>(StringComparer.Ordinal);
            var unusedVariables = new HashSet<string>(variableLookup.Keys, StringComparer.Ordinal);

            foreach (var argument in arguments)
            {
                var varName = argument.VariableName;

                if (variableLookup.TryGetValue(varName, out var variable) is false)
                {
                    _diagnostics.Add(MissingVariableDiagnostic.CreateMetadata(argument.Name, varName, argument.Reference.GetLocation()));
                    continue;
                }

                // Mark as used
                unusedVariables.Remove(varName);
            }

            // Any remaining variables were not used by any argument
            foreach (var variableName in unusedVariables)
            {
                var variable = variableLookup[variableName];
                _diagnostics.Add(UnusedVariableDiagnostic.CreateMetadata(variableName, variable.Reference.GetLocation()));
            }
        }


        private void UpdateQueryFromAttributes(QueryMetadata query, ImmutableArray<AttributeData> attributes)
        {
            Dictionary<string, VariableMetadata> variables = new(StringComparer.Ordinal);

            foreach (var attribute in attributes)
            {
                switch (attribute.ToFullName())
                {
                    case AttributeNames.QUERY:
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
                                    query.Options.PropertyNamingPolicy =jsonPropertyNaming;
                                    break;
                                case nameof(QueryAttribute.Formatting) when argument.Value.Value is SourceFormatting sourceFormatting:
                                    query.Options.Formatting = sourceFormatting;
                                    break;
                            }
                        }
                        break;

                    case AttributeNames.VARIABLE:
                        var (variablName, graphType) = attribute.GetConstructorArguments();
                        var defaultValue = attribute.TryGetNamedArgument<object>(nameof(VariableAttribute.DefaultValue), out var value) ? value : null;

                        if (variables.ContainsKey(variablName) == false)
                        {
                            variables.Add(variablName, new VariableMetadata(variablName, graphType, defaultValue, attribute.ApplicationSyntaxReference));
                        }
                    
                        break;
                }
            }

            query.Variables = [.. variables.Values];
        }

        private (TypeMetadata, List<ArgumentMetadata>) ParseTypeMetadata(INamedTypeSymbol type)
        {
            if (_typeCache.ContainsKey(type))
            {
                return _typeCache[type];
            }

            INamedTypeSymbol? current = type;

            Dictionary<string, MemberMetadata> members = new(StringComparer.Ordinal);
            List<ArgumentMetadata> arguments = [];

            while (current?.Name is not null or "Object" or "ValueType")
            {
                foreach (var member in current.GetMembers())
                {
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

                    UpdateMemberFromInheritedAttributes(metadata, attributes);
                }

                current = current.BaseType;
            }

            var typeMembers = members.Values.Where(m => m.IsSerializable && m.Ignore is not true).ToArray();
            var typeMetadata = new TypeMetadata(type.ToDisplayString(), [.. typeMembers]);

            var result = (typeMetadata, arguments);
            _typeCache[type] = result;
            return result;
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
            Dictionary<string, ArgumentMetadata> arguments = new(StringComparer.Ordinal);
            HashSet<string> inlineFragments = new(StringComparer.Ordinal);

            foreach (var attribute in attributes)
            {
                switch (attribute.ToFullName())
                {
                    case AttributeNames.ARGUMENT:
                        var (name, variableName) = attribute.GetConstructorArguments();

                        if (arguments.ContainsKey(name) == false)
                        {
                            var argMetadata = new ArgumentMetadata(name, variableName, attribute.ApplicationSyntaxReference);
                            arguments.Add(name, argMetadata);
                        }
                        break;

                    case AttributeNames.ALIAS_OF:
                        metadata.AliasOf = attribute.GetConstructorArgument();
                        break;

                    case AttributeNames.ON:
                        inlineFragments.Add(attribute.GetConstructorArgument());
                        break;
                        
                }
            }

            metadata.Arguments = [.. arguments.Values];
            metadata.On = [.. inlineFragments];
        }

        private void UpdateMemberFromInheritedAttributes(MemberMetadata metadata, ImmutableArray<AttributeData> attributes)
        {
            foreach (var attribute in attributes)
            {
                switch (attribute.ToFullName())
                {
                    case AttributeNames.JSON_PROPERTY when metadata.OverrideName is null:
                        metadata.OverrideName = attribute.GetConstructorArgument();
                        break;

                    case AttributeNames.JSON_IGNORE when metadata.Ignore is null:
                        var ignoreCondition = attribute.TryGetNamedArgument<int>(nameof(JsonIgnoreAttribute.Condition), out var condition)
                            ? (JsonIgnoreCondition)condition
                            : JsonIgnoreCondition.Always;

                        metadata.Ignore = ignoreCondition is not JsonIgnoreCondition.Never;
                        break;
                }
            }
        }
    }
}