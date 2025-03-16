using System;

namespace QueryByShape
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public class OnAttribute : Attribute
    {
        internal string Name { get; }

        public OnAttribute(string name)
        {
            this.Name = name;
        }
    }
}
