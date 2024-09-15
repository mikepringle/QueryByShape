using System;

namespace QueryByShape
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class AliasOfAttribute : Attribute
    {
        internal string Name { get; }

        public AliasOfAttribute(string name)
        {
            this.Name = name;
        }
    }
}
