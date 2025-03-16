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
        private readonly Dictionary<string, (TypeMetadata, List<ArgumentMetadata>)> _typeCache = [];

        public static EquatableArray<ParseResult> Process(ImmutableArray<(TypeDeclarationSyntax declaration, SemanticModel semanticModel)> contexts, NamedTypeSymbols symbols, CancellationToken cancellationToken)
        {
            var items = new ParseResult[contexts.Length];
            var parser = new QueryParser(symbols);

            for (int i = 0; i < contexts.Length; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }

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

            var query = new QueryMetadata(declaredSymbol.Name, declaredSymbol.GetNamespace());
            query.Type = ParseTypeMetadata(declaredSymbol, query.Options, out var queryArguments);
            UpdateQueryFromAttributes(query, declaredSymbol.GetAttributes());
            ValidateVariables(query.Variables, queryArguments);
            
            return (query, [.. _diagnostics]);
        }

        public void ValidateVariables(IEnumerable<VariableMetadata>? variables, IList<ArgumentMetadata> arguments)
        {
            var variableLookup = variables?.ToDictionary(v => v.Name) ?? [];
            var variableUsage = variableLookup.Keys.ToHashSet();

            foreach (var argument in arguments)
            {
                if (variableLookup.ContainsKey(argument.VariableName) is false)
                {
                    _diagnostics.Add(MissingVariableDiagnostic.CreateMetadata(argument.Name, argument.VariableName, argument.Reference.GetLocation()));
                }
                else if (variableUsage.Contains(argument.VariableName))
                {
                    variableUsage.Remove(argument.VariableName);
                }
            }

            foreach (var variableName in variableUsage)
            {
                _diagnostics.Add(UnusedVariableDiagnostic.CreateMetadata(variableName, variableLookup[variableName].Reference.GetLocation()));
            }
        }


        private void UpdateQueryFromAttributes(QueryMetadata query, ImmutableArray<AttributeData> attributes)
        {
            Dictionary<string, VariableMetadata> variables = [];

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
                                case nameof(QueryAttribute.IncludeFields):
                                    query.Options.IncludeFields = bool.Parse(argument.Value.Value!.ToString());
                                    break;
                                case nameof(QueryAttribute.PropertyNamingPolicy):
                                    query.Options.PropertyNamingPolicy = (JsonPropertyNaming)Enum.Parse(typeof(JsonPropertyNaming), argument.Value.Value!.ToString());
                                    break;
                                case nameof(QueryAttribute.Formatting):
                                    query.Options.Formatting = (SourceFormatting)Enum.Parse(typeof(SourceFormatting), argument.Value.Value!.ToString());
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

        private TypeMetadata ParseTypeMetadata(INamedTypeSymbol type, QueryOptions options, out List<ArgumentMetadata> childArguments)
        {
            INamedTypeSymbol? current = type;
            var name = type.ToDisplayString();
            
            if (_typeCache.ContainsKey(name))
            {
                (var metadata, childArguments) = _typeCache[name];
                return metadata;
            }

            Dictionary<string, MemberMetadata> members = [];
            childArguments = [];

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
                            metadata.ChildrenType = ParseTypeMetadata(childrenType!, options, out var innerChildArguments);
                            childArguments.AddRange(innerChildArguments);
                        }

                        UpdateMemberFromBaseAttributes(metadata, attributes);
                        
                        if (metadata.Arguments?.Count > 0)
                        {
                            childArguments.AddRange(metadata.Arguments);
                        }
                    }

                    UpdateMemberFromInheritedAttributes(metadata, attributes);
                }

                current = current.BaseType;
            }

            var typeMembers = members.Values.Where(m => m.IsSerializable && m.Ignore is not true).ToArray();
            var typeMetadata = new TypeMetadata(name, [.. typeMembers]);

            _typeCache[name] = (typeMetadata, childArguments);
            return typeMetadata;
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
            var arguments = new Dictionary<string, ArgumentMetadata>();
            var inlineFragments = new HashSet<string>();

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