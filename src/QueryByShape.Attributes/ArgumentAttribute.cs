using System;
using System.Collections.Generic;
using System.Text;

namespace QueryByShape
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class ArgumentAttribute : Attribute
    {
        internal string Name { get; }

        internal string VariableName { get; }

        public ArgumentAttribute(string name, string variableName)
        {
            this.Name = name;
            this.VariableName = variableName;
        }
    }
}
