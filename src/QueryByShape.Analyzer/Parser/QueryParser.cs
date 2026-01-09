global using ParseResult = (QueryByShape.Analyzer.QueryMetadata Metadata, QueryByShape.Analyzer.EquatableArray<QueryByShape.Analyzer.DiagnosticMetadata>? Diagnostics);
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using QueryByShape.Analyzer.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;

namespace QueryByShape.Analyzer
{
    internal class QueryParser  
    {
        private static readonly StringComparer _stringComparer = StringComparer.Ordinal;
        private readonly NamedTypeSymbols _symbols;
        private readonly List<DiagnosticMetadata> _diagnostics = new();
        private readonly ReferenceSet<string> _variableRefs = new(_stringComparer);
        private CancellationToken _cancellationToken;

        private QueryParser(NamedTypeSymbols symbols, CancellationToken cancellationToken)
        {
            _symbols = symbols;
            _cancellationToken = cancellationToken;
        }

        public static ParseResult Parse(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel, NamedTypeSymbols symbols, CancellationToken cancellationToken)
        {
            var parser = new QueryParser(symbols, cancellationToken);
            var declaredSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration)!;
            return parser.ParseQuery(declaredSymbol);
        }

        private ParseResult ParseQuery(INamedTypeSymbol declaredSymbol)
        {
            var query = new QueryMetadata(declaredSymbol.Name, declaredSymbol.GetNamespace());
            UpdateQueryFromAttributes(query, declaredSymbol.GetAttributes());
            query.Type = ParseTypeMetadata(declaredSymbol);
            ValidateVariables(query.Variables);

            return (query, new (_diagnostics.ToArray()));
        }

        private void ValidateVariables(EquatableArray<VariableMetadata>? variables)
        {
            if (variables == null)
            {
                return;
            }
            
            foreach (var variable in variables)
            {
                if (_variableRefs.IsReferenced(variable.Name) is false)
                {
                    _diagnostics.Add(UnusedVariableDiagnostic.CreateMetadata(variable.Name, variable.Reference.GetLocation()));
                }
            }
        }

        private void UpdateQueryFromAttributes(QueryMetadata query, ImmutableArray<AttributeData> attributes)
        {
            List<VariableMetadata> variables = new();
            
            foreach (var attribute in attributes)
            {
                if (attribute.IsAttributeType(_symbols.QueryAttribute))
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
                else if (attribute.IsAttributeType(_symbols.VariableAttribute))
                { 
                    if (attribute.TryGetConstructorArguments(out string? variableName, out string? graphType) 
                        && _variableRefs.TryAddSource(variableName))
                    {
                        var defaultValue = attribute.TryGetNamedArgument<object>(nameof(VariableAttribute.DefaultValue), out var value) ? value : null;
                        variables.Add(new VariableMetadata(variableName, graphType, defaultValue, attribute.ApplicationSyntaxReference));
                    }
                }
            }

            query.Variables = new(variables.ToArray());
        }

        private TypeMetadata ParseTypeMetadata(INamedTypeSymbol type)
        {
            INamedTypeSymbol? current = type;

            Dictionary<string, MemberMetadata> members = new(_stringComparer); 
            HashSet<string> skippedMembers = new(_stringComparer); 

            while (current != null && current.SpecialType is not SpecialType.System_Object and not SpecialType.System_ValueType)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                foreach (var member in current.GetMembers())
                {
                    var memberName = member.Name;
                    
                    if ((member.Kind != SymbolKind.Property && member.Kind != SymbolKind.Field) || member.IsImplicitlyDeclared || skippedMembers.Contains(memberName))
                    {
                        continue;
                    }

                    var attributes = member.GetAttributes();
        
                    if (!members.TryGetValue(memberName, out var metadata))
                    {
                        if (!_symbols.TryGetSerializableMemberType(member, out var memberType))
                        {
                            skippedMembers.Add(memberName);
                            continue;
                        }

                        metadata = new MemberMetadata(member.Name, member.Kind);
                        members[memberName] = metadata;

                        if (_symbols.TryGetChildrenType(memberType!, out var childrenType))
                        {
                            metadata.ChildrenType = ParseTypeMetadata(childrenType);
                        }

                        UpdateMemberFromBaseAttributes(metadata, attributes);
                    }

                    UpdateMemberFromInheritedAttributes(metadata, attributes);
                        
                    if (metadata.Ignore == true)
                    {
                        members.Remove(memberName);
                        skippedMembers.Add(memberName);
                        continue;
                    }                    
                }

                current = current.BaseType;
            }

            var result = new TypeMetadata(new (members.Values.ToArray()));
            return result;
        }

        private void UpdateMemberFromBaseAttributes(MemberMetadata metadata, ImmutableArray<AttributeData> attributes)
        {
            Dictionary<string, ArgumentMetadata> arguments = new(_stringComparer);
            HashSet<string> inlineFragments = new(_stringComparer); 
            
            foreach (var attribute in attributes)
            {
                if (attribute.IsAttributeType(_symbols.ArgumentAttribute))
                {
                    if (attribute.TryGetConstructorArguments(out string? name, out string? variableName)
                        && !arguments.ContainsKey(name))
                    {
                        if (_variableRefs.TryMarkReferenced(variableName))
                        {
                            var argMetadata = new ArgumentMetadata(name, variableName);
                            arguments[name] = argMetadata;
                        }
                        else
                        {
                            _diagnostics.Add(MissingVariableDiagnostic.CreateMetadata(name, variableName, attribute.GetLocation()));
                            continue;
                        }
                    }
                }
                else if (attribute.IsAttributeType(_symbols.AliasOfAttribute))
                {
                    metadata.AliasOf = attribute.TryGetConstructorArgument(out string? alias) ? alias : null;
                }
                else if (attribute.IsAttributeType(_symbols.OnAttribute))
                {
                    if (attribute.TryGetConstructorArgument(out string? on))
                    {
                        inlineFragments.Add(on);
                    }
                }
            }

            metadata.Arguments = new(arguments.Values.ToArray());
            metadata.On = new(inlineFragments.ToArray());
        }

        private void UpdateMemberFromInheritedAttributes(MemberMetadata metadata, ImmutableArray<AttributeData> attributes)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.IsAttributeType(_symbols.JsonPropertyAttribute))
                {
                    metadata.OverrideName ??= attribute.TryGetConstructorArgument(out string? overrideName) ? overrideName : null;
                }
                else if (attribute.IsAttributeType(_symbols.JsonIgnoreAttribute))
                { 
                    var ignoreCondition = attribute.TryGetNamedArgument<int>(nameof(JsonIgnoreAttribute.Condition), out var condition)
                            ? (JsonIgnoreCondition)condition
                            : JsonIgnoreCondition.Always;

                    metadata.Ignore ??= ignoreCondition != JsonIgnoreCondition.Never;       
                }
            }
        }
    }
}