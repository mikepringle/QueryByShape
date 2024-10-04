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
using System.Text.Json.Serialization;
using System.Threading;

namespace QueryByShape.Analyzer
{
    internal class QueryParser(NamedTypeSymbols symbols)
    {
        private readonly List<DiagnosticMetadata> _diagnostics = new();
        Dictionary<string, (TypeMetadata, List<ArgumentMetadata>)> _typeCache = new();

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

            return new(items);
        }

        public ParseResult Parse(TypeDeclarationSyntax typeDeclaration, INamedTypeSymbol declaredSymbol)
        {
            var query = new QueryMetadata(declaredSymbol.Name, declaredSymbol.GetNamespace());
            
            if (typeDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)) is false) 
            {
                _diagnostics.Add(QueryMustBePartialDiagnostic.Create(declaredSymbol.Name, typeDeclaration.GetLocation()));
            }

            (query.Type, var queryArguments) = ParseTypeMetadata(declaredSymbol, query.Options);
            UpdateQueryFromAttributes(query, declaredSymbol.GetAttributes());
            ValidateVariables(query.Variables, queryArguments);
            
            return (query, new(_diagnostics.ToArray()));
        }

        public void ValidateVariables(IEnumerable<VariableMetadata>? variables, IList<ArgumentMetadata> arguments)
        {
            var variableLookup = variables?.ToDictionary(v => v.Name) ?? new();
            var variableUsage = variableLookup.Keys.ToHashSet();

            foreach (var argument in arguments)
            {
                if (variableLookup.ContainsKey(argument.VariableName) is false)
                {
                    _diagnostics.Add(MissingVariableDiagnostic.Create(argument.Name, argument.VariableName, argument.Reference.GetLocation()));
                }
                else if (variableUsage.Contains(argument.VariableName))
                {
                    variableUsage.Remove(argument.VariableName);
                }
            }

            foreach (var variableName in variableUsage)
            {
                _diagnostics.Add(UnusedVariableDiagnostic.Create(variableName, variableLookup[variableName].Reference.GetLocation()));
            }
        }


        private void UpdateQueryFromAttributes(QueryMetadata query, ImmutableArray<AttributeData> attributes)
        {
            Dictionary<string, VariableMetadata> variables = new();

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

                        if (variables.ContainsKey(variablName))
                        {
                            _diagnostics.Add(DuplicateVariableDiagnostic.Create(variablName, attribute.GetLocation()));
                        }
                        else
                        {
                            variables.Add(variablName, new VariableMetadata(variablName, graphType, defaultValue, attribute.ApplicationSyntaxReference));
                        }
                    
                        break;
                }
            }

            query.Variables = variables.Values.ToEquatableArray();
        }

        private (TypeMetadata, List<ArgumentMetadata>) ParseTypeMetadata(INamedTypeSymbol type, QueryOptions options)
        {
            var fromBaseType = false;
            INamedTypeSymbol? current = type;
            var name = type.ToDisplayString();
            
            if (_typeCache.ContainsKey(name))
            {
                return _typeCache[name];
            }

            Dictionary<string, MemberMetadata> members = new();
            List<ArgumentMetadata> arguments = new();

            while (current?.Name is not null or "Object" or "ValueType")
            {
                foreach (var member in current.GetMembers())
                {
                    var memberName = member.Name;

                    if (!members.TryGetValue(memberName, out var metadata))
                    {
                        var (isSerializable, childrenType) = symbols.GetMemberInfo(member);

                        metadata = new MemberMetadata(memberName, member.Kind);
                        metadata.IsSerializable = isSerializable;
                        
                        if (childrenType != null)
                        {
                            (metadata.ChildrenType, var childArguments) = ParseTypeMetadata(childrenType, options);
                            arguments.AddRange(childArguments);

                        }

                        members[memberName] = metadata;
                    }

                    if (metadata.IsSerializable)
                    {
                        UpdateMemberFromAttributes(metadata, member.GetAttributes(), fromBaseType);
                        
                        if (fromBaseType is false && metadata.Arguments is not null)
                        {
                            arguments.AddRange(metadata.Arguments);
                        }
                    }
                }

                fromBaseType = true;
                current = current.BaseType;
            }

            var typeMembers = members.Values.Where(m => m.IsSerializable && m.Ignore is not true).ToArray();
            var typeMetadata = new TypeMetadata(name, typeMembers.ToEquatableArray());

            var result = (typeMetadata, arguments);
            _typeCache[name] = result;
            return result;
        }

        private void UpdateMemberFromAttributes(MemberMetadata metadata, ImmutableArray<AttributeData> attributes, bool isMemberFromBase)
        {
            var arguments = new Dictionary<string, ArgumentMetadata>();
            
            foreach (var attribute in attributes)
            {
                switch (attribute.ToFullName())
                {
                    case AttributeNames.JSON_PROPERTY when metadata.OverrideName is null:
                        metadata.OverrideName = attribute.GetConstructorArgument();
                        break;

                    case AttributeNames.JSON_IGNORE when metadata.Ignore is null:
                        var ignoreCondition = attribute.TryGetNamedArgument<int>(nameof(JsonIgnoreAttribute.Condition), out var condition)
                            ? (JsonIgnoreCondition) condition
                            : JsonIgnoreCondition.Always;

                        metadata.Ignore = ignoreCondition is not JsonIgnoreCondition.Never;
                        break;

                    case AttributeNames.ARGUMENT when isMemberFromBase is false:
                        var (name, variableName) = attribute.GetConstructorArguments();

                        if (arguments.ContainsKey(name))
                        {
                            _diagnostics.Add(DuplicateArgumentDiagnostic.Create(name, attribute.GetLocation()));
                        }
                        else
                        {
                            var argMetadata = new ArgumentMetadata(name, variableName, attribute.ApplicationSyntaxReference);
                            arguments.Add(name, argMetadata);
                        }
                        break;

                    case AttributeNames.ALIAS_OF when isMemberFromBase is false:
                        metadata.AliasOf = attribute.GetConstructorArgument();
                        break;
                }
            }

            if (isMemberFromBase is false)
            {
                metadata.Arguments = arguments.Values.ToEquatableArray();
            }
        }
    }
}