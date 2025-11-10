using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace QueryByShape.Analyzer
{
    internal class QueryEmitter(QueryMetadata query)
    {
        private readonly SourceBuilder _sb = new(query.Options.Formatting);
        private readonly JsonPropertyNaming _propertyNaming = query.Options.PropertyNamingPolicy;

        public static void EmitSource(SourceProductionContext ctx, ParseResult result)
        {
            var (query, diagnostics) = result;
            var emitter = new QueryEmitter(query);

            if (diagnostics?.Any(d => d.Descriptor.DefaultSeverity is DiagnosticSeverity.Error) == true)
            {
                return;
            }

            string graphQlQuery = emitter.EmitQuery();
            var generatedClass = GeneratorTemplate.Build(graphQlQuery, query.NamespaceName, query.TypeName);
            ctx.AddSource($"QueryByShape.{query.TypeFullName}.Query.g.cs", SourceText.From(generatedClass, Encoding.UTF8));
        }
        private string EmitQuery()
        {
            _sb.Append("query ");
            _sb.Append(FormatName(query.Name ?? query.TypeName));

            if (query.Variables?.Count > 0)
            {
                var variables = query.Variables.Value;
                _sb.Append("(");

                for (int i = 0; i < variables.Count; i++)
                {
                    var variable = variables[i];
                
                    if (i != 0)
                    {
                        _sb.Append(",");
                    }
                    
                    _sb.Append(variable.Name);
                    _sb.Append(":");
                    _sb.Append(variable.GraphType);
                    
                    if (variable.DefaultValue != null)
                    {
                        _sb.Append(" = ");
                        _sb.Append(JsonSerializer.Serialize(variable.DefaultValue));
                    }
                }
                
                _sb.Append(")");
            }

            EmitType(query.Type, query.Options);

            return _sb.ToString();
        }

        private string FormatName(string name)
        {
            if (_propertyNaming == JsonPropertyNaming.CamelCase)
            {
                return JsonNamingPolicy.CamelCase.ConvertName(name);
            }

            return name;
        }

        private void EmitType(TypeMetadata typeMetadata, QueryOptions options)
        {
            var members = typeMetadata.Members;

            var withoutFragments =  new List<MemberMetadata>(members.Count);
            var withFragments = new Dictionary<string, List<MemberMetadata>>();
            
            foreach (var member in typeMetadata.Members)
            {
                if (member.Ignore == true || (member.Kind == SymbolKind.Field && !options.IncludeFields))
                {
                    continue;
                }

                if (member.On?.Count > 0)
                {
                    foreach (var fragment in member.On)
                    {
                        if (!withFragments.TryGetValue(fragment, out var fragmentMembers))
                        {
                            fragmentMembers = new List<MemberMetadata>();
                            withFragments[fragment] = fragmentMembers;
                        }

                        fragmentMembers.Add(member);
                    }
                }
                else
                {
                    withoutFragments.Add(member);
                }
            }

            _sb.AppendStartBlock();

            EmitMembers(withoutFragments, options);

            if (withFragments.Count > 0)
            {
                foreach (var fragment in withFragments)
                {
                    _sb.AppendLine();
                    _sb.Append($"... on {fragment.Key}");
                    _sb.AppendStartBlock();
                    EmitMembers(fragment.Value, options);
                    _sb.AppendEndBlock();
                }
            }

            _sb.AppendEndBlock();
        }

        private void EmitMembers(List<MemberMetadata> members, QueryOptions options)
        {
            foreach (var member in members)
            {
                _sb.AppendLine();
                _sb.Append(member.OverrideName ?? FormatName(member.Name));

                if (member.AliasOf != null)
                {
                    _sb.Append(":");
                    _sb.Append(member.AliasOf);
                }

                if (member.Arguments?.Count > 0)
                {
                    var arguments = member.Arguments.Value;
                    _sb.Append("(");

                    for (int i = 0; i < arguments.Count; i++)
                    {
                        var argument = arguments[i];

                        if (i != 0)
                        {
                            _sb.Append(",");
                        }

                        _sb.Append(argument.Name);
                        _sb.Append(":");
                        _sb.Append(argument.VariableName);
                    }

                    _sb.Append(")");
                }

                if (member.ChildrenType?.Members.Count > 0)
                {
                    EmitType(member.ChildrenType, options);
                }
            }
        }
    }
}
