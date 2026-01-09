using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace QueryByShape.Analyzer
{
    internal static class QueryEmitter
    {
        public static void EmitQuery(QueryMetadata query, StringBuilder builder)
        {
            SourceBuilder sb = new(query.Options.Formatting, builder);

            sb.Append("query ");
            
            EmitName(query.Name ?? query.TypeName, query.Options.PropertyNamingPolicy, sb);

            if (query.Variables?.Count > 0)
            {
                var variables = query.Variables.Value;
                sb.Append('(');

                for (int i = 0; i < variables.Count; i++)
                {
                    var variable = variables[i];
                
                    if (i != 0)
                    {
                        sb.Append(',');
                    }
                    
                    sb.Append(variable.Name);
                    sb.Append(':');
                    sb.Append(variable.GraphType);
                    
                    if (variable.DefaultValue != null)
                    {
                        sb.Append(" = ");
                        sb.Append(JsonSerializer.Serialize(variable.DefaultValue));
                    }
                }
                
                sb.Append(')');
            }

            EmitType(query.Type!, query.Options, sb);
        }

        private static void EmitName(string name, JsonPropertyNaming namingPolicy, SourceBuilder sb)
        {
            if (namingPolicy == JsonPropertyNaming.CamelCase)
            {
                sb.Append(JsonNamingPolicy.CamelCase.ConvertName(name));
            }
            else
            {
                sb.Append(name);
            }
        }

        private static void EmitType(TypeMetadata typeMetadata, QueryOptions options, SourceBuilder sb)
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

            sb.AppendStartBlock();

            EmitMembers(withoutFragments, options, sb);

            if (withFragments.Count > 0)
            {
                foreach (var fragment in withFragments)
                {
                    sb.AppendLine();
                    sb.Append("... on ");
                    sb.Append(fragment.Key);
                    
                    sb.AppendStartBlock();
                    EmitMembers(fragment.Value, options, sb);
                    sb.AppendEndBlock();
                }
            }

            sb.AppendEndBlock();
        }

        private static void EmitMembers(List<MemberMetadata> members, QueryOptions options, SourceBuilder sb)
        {
            foreach (var member in members)
            {
                sb.AppendLine();

                if (member.OverrideName != null)
                {
                    sb.Append(member.OverrideName);
                }
                else
                {
                     EmitName(member.Name, options.PropertyNamingPolicy, sb);
                }

                if (member.AliasOf != null)
                {
                    sb.Append(':');
                    sb.Append(member.AliasOf);
                }

                if (member.Arguments?.Count > 0)
                {
                    var arguments = member.Arguments.Value;
                    sb.Append('(');

                    for (int i = 0; i < arguments.Count; i++)
                    {
                        var argument = arguments[i];

                        if (i != 0)
                        {
                            sb.Append(',');
                        }

                        sb.Append(argument.Name);
                        sb.Append(':');
                        sb.Append(argument.VariableName);
                    }

                    sb.Append(')');
                }

                if (member.ChildrenType?.Members.Count > 0)
                {
                    EmitType(member.ChildrenType, options, sb);
                }
            }
        }
    }
}
