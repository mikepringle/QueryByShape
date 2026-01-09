using System;
using System.Text;

namespace QueryByShape.Analyzer
{
    internal class SourceBuilder(SourceFormatting formatting, StringBuilder sb)
    {
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

        internal void Append(char character)
        {
            sb.Append(character);
        }

        public override string ToString() => sb.ToString();
    }

}
