using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using QueryByShape.Analyzer.Diagnostics;
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
            // variables dynamic or static set?  use case
            // variables with object type (sorting)
            
            _sb.Append("query ");
            _sb.Append(FormatName(query.Name ?? query.TypeName));

            EmitParameters(query.Variables?.Select(v => (v.Name, v.GraphType)));
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
            var members = typeMetadata.Members;
            
            if (members is null)
            {
                return;
            }

            var filtered = options.IncludeFields == false ? members.Value.Where(m => m.Kind == SymbolKind.Property) : members.Value;

            _sb.AppendLine("{");
            foreach (var member in filtered)
            {
                _sb.AppendIndent(depth);
                _sb.Append(member.OverrideName ?? FormatName(member.Name));

                if (member.AliasOf != null)
                {
                    _sb.Append(":");
                    _sb.Append(member.AliasOf);
                }

                EmitParameters(member.Arguments?.Select(a => (a.Name, a.VariableName)));
                    
                if (member.ChildrenType?.Members?.Count > 0)
                {
                    EmitType(member.ChildrenType, options, depth + 1);
                }
                else
                {
                    _sb.AppendLine(string.Empty);
                }
            }
            _sb.AppendLine("}", depth - 1);
        }

        internal void EmitParameters(IEnumerable<(string, string)>? items)
        {
            if (items == null || !items.Any())
            {
                return;
            }

            var isFirst = true;

            _sb.Append("(");

            foreach (var item in items)
            {
                var (key, value) = item;

                if (isFirst == false)
                {
                    _sb.Append(",");
                }

                _sb.Append($"{key}:{value}");
                isFirst = false;
            }
            _sb.Append(")");
        }
    }
}
