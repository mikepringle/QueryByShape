using System;

namespace QueryByShape
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true)]
    public class QueryAttribute : Attribute
    {
        public string OperationName { get; set; } = null;

        public bool IncludeFields { get; set; } = false;

        public JsonPropertyNaming PropertyNamingPolicy { get; set; } = JsonPropertyNaming.CamelCase;

        public SourceFormatting Formatting { get; set; } = SourceFormatting.Minified;
    }

    public enum JsonPropertyNaming
    {
        None,
        CamelCase
    }

    public enum SourceFormatting
    {
        Minified,
        Pretty
    }
}
