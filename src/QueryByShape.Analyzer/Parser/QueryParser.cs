global using ParseResult = (QueryByShape.Analyzer.QueryMetadata Metadata, QueryByShape.Analyzer.EquatableArray<QueryByShape.Analyzer.DiagnosticMetadata>? Diagnostics);
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using QueryByShape.Analyzer.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;

namespace QueryByShape.Analyzer
{
    internal class QueryParser(NamedTypeSymbols symbols)
    {
        private static readonly StringComparer _stringComparer = StringComparer.Ordinal;

        public ParseResult Parse(TypeDeclarationSyntax typeDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var declaredSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration)!;

            var (types, queryArguments) = ParseTypeMetadata(declaredSymbol, cancellationToken);
            
            var query = new QueryMetadata(declaredSymbol.Name, declaredSymbol.GetNamespace(), types);
            
            UpdateQueryFromAttributes(query, declaredSymbol.GetAttributes());
            ValidateVariables(query.Variables, queryArguments, out var diagnostics);
            
            return (query, diagnostics != null ? [.. diagnostics] : null);
        }

        public void ValidateVariables(EquatableArray<VariableMetadata>? variables, IList<ArgumentMetadata> arguments, out List<DiagnosticMetadata>? diagnostics)
        {
            diagnostics = null;

            // Nothing to validate
            if ((variables == null || variables?.Count == 0) && arguments.Count == 0)
            {
                return;
            }

            var variableRefs = new ReferenceSet<string, VariableMetadata>(variables, v => v.Name, StringComparer.Ordinal);

            foreach (var argument in arguments)
            {
                var varName = argument.VariableName;

                if (!variableRefs.TryMarkReferenced(varName))
                {
                    diagnostics ??= new List<DiagnosticMetadata>();
                    diagnostics.Add(MissingVariableDiagnostic.CreateMetadata(argument.Name, varName, argument.Reference.GetLocation()));
                    continue;
                }
            }

            // Any remaining variables were not used by any argument
            foreach (var variable in variableRefs.GetUnreferenced())
            {
                diagnostics ??= new List<DiagnosticMetadata>();
                diagnostics.Add(UnusedVariableDiagnostic.CreateMetadata(variable.Name, variable.Reference.GetLocation()));
            }

        }


        private void UpdateQueryFromAttributes(QueryMetadata query, ImmutableArray<AttributeData> attributes)
        {
            List<VariableMetadata> variables = [];
            HashSet<string> existingVariables = new(_stringComparer);

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
                        && !existingVariables.Contains(variableName))
                    {
                        var defaultValue = attribute.TryGetNamedArgument<object>(nameof(VariableAttribute.DefaultValue), out var value) ? value : null;
                        variables.Add(new VariableMetadata(variableName, graphType, defaultValue, attribute.ApplicationSyntaxReference));
                        existingVariables.Add(variableName);
                    }
                }
            }

            query.Variables = [.. variables];
        }

        private (TypeMetadata, List<ArgumentMetadata>) ParseTypeMetadata(INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            INamedTypeSymbol? current = type;
            Dictionary<string, MemberMetadata> members = new(_stringComparer);
            HashSet<string> skippedMembers = new(_stringComparer);
            List<ArgumentMetadata> arguments = [];

            while (current != null && current.SpecialType is not SpecialType.System_Object and not SpecialType.System_ValueType)
            {
                cancellationToken.ThrowIfCancellationRequested();

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
                        if (!TryGetSerializableMemberType(member, out var memberType))
                        {
                            skippedMembers.Add(memberName);
                            continue;
                        }

                        metadata = new MemberMetadata(member.Name, member.Kind);
                        members[memberName] = metadata;

                        if (symbols.TryGetChildrenType(memberType!, out var childrenType))
                        {
                            var (childMetadata, childArguments) = ParseTypeMetadata(childrenType, cancellationToken);
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
                        
                    if (metadata.Ignore == true)
                    {
                        members.Remove(memberName);
                        skippedMembers.Add(memberName);
                        continue;
                    }
                    else if (metadata.Ignore != null && metadata.OverrideName != null)
                    {
                        continue;
                    }                    
                }

                current = current.BaseType;
            }

            var typeMetadata = new TypeMetadata(type.ToDisplayString(), [.. members.Values]);
            
            return (typeMetadata, arguments);
        }

        private bool TryGetSerializableMemberType(ISymbol member, [NotNullWhen(true)] out INamedTypeSymbol? memberType)
        {
            memberType = member switch
            {
                IPropertySymbol property when symbols.IsPropertySerializable(property) => property.Type,
                IFieldSymbol field when symbols.IsFieldSerializable(field) => field.Type,
                _ => null
            } as INamedTypeSymbol;

            if (memberType == null || !symbols.IsTypeSerializable(memberType))
            {
                memberType = null;
                return false;
            }

            return true;
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
                        && !arguments.ContainsKey(name))
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
                if (attribute.IsAttributeType(symbols.JsonPropertyAttribute))
                {
                    metadata.OverrideName ??= attribute.TryGetConstructorArgument(out string? overrideName) ? overrideName : null;
                }
                else if (attribute.IsAttributeType(symbols.JsonIgnoreAttribute))
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