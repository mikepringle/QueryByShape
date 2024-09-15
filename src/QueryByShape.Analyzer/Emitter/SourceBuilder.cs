using System.Text;

namespace QueryByShape.Analyzer
{
    internal class SourceBuilder(SourceFormatting formatting)
    {
        private StringBuilder sb = new StringBuilder();

        public void Append(string text) 
        {
            sb.Append(text);
        }

        public void AppendLine(string text, int depth = 0)
        {
            AppendIndent(depth);
            
            if (formatting == SourceFormatting.Minified)
            {
                sb.Append(text);
            }
            else
            {
                sb.AppendLine(text);
            }
        }

        public void AppendIndent(int depth)
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

        public override string ToString() => sb.ToString();
    }
}
