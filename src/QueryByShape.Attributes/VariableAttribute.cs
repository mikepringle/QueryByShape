using System;
using System.Collections.Generic;
using System.Text;

namespace QueryByShape
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class VariableAttribute : Attribute
    {
        internal string Name { get; }

        internal string GraphType { get; }

        public object DefaultValue { get; set; }

        public VariableAttribute(string name, string graphType)
        {
            if (!name.StartsWith("$"))
            {
                throw new ArgumentException("Variables must start with $");
            }

            Name = name;
            GraphType = graphType;
        }
    }
}
