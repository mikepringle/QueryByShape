using System;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace QueryByShape.Analyzer
{
    internal class SourceBuilder(SourceFormatting formatting)
    {
        private readonly StringBuilder sb = new StringBuilder();
        private int depth = 0;
        
        internal void AppendStartBlock()
        {
            sb.Append(" {");
            depth++;
        }

        internal void AppendEndBlock()
        {
            depth--;
            AppendLine();
            sb.Append("}");
        }
        
        internal void AppendLine()
        {
            if (formatting == SourceFormatting.Minified)
            {
                sb.Append(" ");
            }
            else
            {
                sb.AppendLine();
                AppendIndent();
            }
        }

        internal void AppendIndent()
        {
            if (formatting == SourceFormatting.Minified)
            {
                sb.Append(" ");
            }
            else if (depth > 0)
            {
                sb.Append(' ', depth * 4);
            }
        }

        internal void Append(string text)
        {
            sb.Append(text);
        }

        internal void AppendParentheses(string[]? values)
        {
            if (values == null || values.Length == 0)
            {
                return;
            }

            sb.Append("(");
            sb.AppendJoin(",", values);
            sb.Append(")");
        }

        public override string ToString() => sb.ToString();
    }

}
