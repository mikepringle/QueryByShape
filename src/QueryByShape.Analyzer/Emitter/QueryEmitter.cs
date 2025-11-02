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

            if (diagnostics.Any(d => d.Descriptor.DefaultSeverity is DiagnosticSeverity.Error) is false)
            {
                string graphQlQuery = emitter.EmitQuery();
                var generatedClass = GeneratorTemplate.Build(graphQlQuery, query.NamespaceName, query.TypeName);
                ctx.AddSource($"QueryByShape.{query.TypeFullName}.Query.g.cs", SourceText.From(generatedClass, Encoding.UTF8));
            }
        }
        private string EmitQuery()
        {
            _sb.Append("query ");
            _sb.Append(FormatName(query.Name ?? query.TypeName));

            var variables = query.Variables?.Select(v => v.DefaultValue == null ? $"{v.Name}:{v.GraphType}" : $"{v.Name}:{v.GraphType} = {JsonSerializer.Serialize(v.DefaultValue)}").ToArray();
            _sb.AppendParentheses(variables);
            
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
            if (typeMetadata.Members is null)
            {
                return;
            }

            var (members, fragmentMembers) = typeMetadata.Members.Value.Partition(m => m.On?.Count is not > 0);

            _sb.AppendStartBlock();

            EmitMembers(members, options);

            if (fragmentMembers.Count > 0)
            {
                var fragments = fragmentMembers
                                    .SelectMany(m => m.On!.Value, (m, o) => (fragment: o, member: m))
                                    .GroupBy(m => m.fragment, m => m.member);

                foreach (var fragment in fragments)
                {
                    _sb.AppendLine();
                    _sb.Append($"... on {fragment.Key}");
                    _sb.AppendStartBlock();
                    EmitMembers(fragment, options);
                    _sb.AppendEndBlock();
                }
            }

            _sb.AppendEndBlock();
        }

        private void EmitMembers(IEnumerable<MemberMetadata> members, QueryOptions options)
        {
            var filtered = options.IncludeFields == false ? members.Where(m => m.Kind == SymbolKind.Property) : members;

            foreach (var member in filtered)
            {
                _sb.AppendLine();
                _sb.Append(member.OverrideName ?? FormatName(member.Name));

                if (member.AliasOf != null)
                {
                    _sb.Append(":");
                    _sb.Append(member.AliasOf);
                }

                var arguments = member.Arguments?.Select(a => $"{a.Name}:{a.VariableName}").ToArray();
                _sb.AppendParentheses(arguments);

                if (member.ChildrenType?.Members?.Count > 0)
                {
                    EmitType(member.ChildrenType, options);
                }
            }
        }
    }
}
