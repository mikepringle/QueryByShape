using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using QueryByShape.Analyzer.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
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
            // variables dynamic or static set?  use case
            // variables with object type (sorting)
            
            _sb.Append("query ");
            _sb.Append(FormatName(query.Name ?? query.TypeName));

            EmitParameters(query.Variables?.Select(v => v.DefaultValue == null ? $"{v.Name}:{v.GraphType}" : $"{v.Name}:{v.GraphType} = {JsonSerializer.Serialize(v.DefaultValue)}"));
            EmitType(query.Type!, query.Options, 1);
            
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

        private void EmitType(TypeMetadata typeMetadata, QueryOptions options, int depth)
        {
            if (typeMetadata.Members is null)
            {
                return;
            }

            var (members, fragmentMembers) = typeMetadata.Members.Value.Partition(m => m.On?.Count is not > 0);

            _sb.AppendLine("{");

            EmitMembers(members, options, depth);

            if (fragmentMembers.Count > 0)
            {
                var fragments = fragmentMembers
                                    .SelectMany(m => m.On!.Value, (m, o) => (fragment: o, member: m))
                                    .GroupBy(m => m.fragment, m => m.member);

                foreach (var fragment in fragments)
                {
                    _sb.AppendLine($"... on {fragment.Key} {{", depth);
                    EmitMembers(fragment, options, depth + 1);
                    _sb.AppendLine("}", depth);
                }
            }

            _sb.AppendLine("}", depth - 1);
        }

        private void EmitMembers(IEnumerable<MemberMetadata> members, QueryOptions options, int depth)
        {
            var filtered = options.IncludeFields == false ? members.Where(m => m.Kind == SymbolKind.Property) : members;

            foreach (var member in filtered)
            {
                _sb.AppendIndent(depth);
                _sb.Append(member.OverrideName ?? FormatName(member.Name));

                if (member.AliasOf != null)
                {
                    _sb.Append(":");
                    _sb.Append(member.AliasOf);
                }

                EmitParameters(member.Arguments?.Select(a => $"{a.Name}:{a.VariableName}"));
                
                if (member.ChildrenType?.Members?.Count > 0)
                {
                    EmitType(member.ChildrenType, options, depth + 1);
                }
                else
                {
                    _sb.AppendLine(string.Empty);
                }
            }
        }

        internal void EmitParameters(IEnumerable<string>? items)
        {
            if (items == null || !items.Any())
            {
                return;
            }

            _sb.Append("(");
            _sb.Append(string.Join(",", items));
            _sb.Append(")");
        }
    }
}
