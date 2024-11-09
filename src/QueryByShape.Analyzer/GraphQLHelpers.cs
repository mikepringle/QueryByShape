using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QueryByShape.Analyzer
{
    internal static class GraphQLHelpers
    {
        public static bool IsValidName(ReadOnlySpan<char> name, out List<string> errors)
        {
            errors = [];

            if (name.Length == 0)
            {
                errors.Add("Name may not be empty");
                return false;
            }

            if (char.IsDigit(name[0]))
            {
                errors.Add("First character of name may not be numeric");
            }

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsLetterOrDigit(c) == false && c is not '_')
                {
                    errors.Add("Must only contain alphanumeric characters or undercores");
                    return false;
                }
            }            

            return errors.Count == 0;
        }
    }
}
