namespace QueryByShape.Analyzer
{
    internal static class AttributeNames
    {
        public const string JSON_PROPERTY = "System.Text.Json.Serialization.JsonPropertyNameAttribute";
        public const string JSON_IGNORE = "System.Text.Json.Serialization.JsonIgnoreAttribute";
        public const string QUERY = $"QueryByShape.{nameof(QueryAttribute)}";
        //public const string MUTATION = $"QueryByShape.{nameof(MutationAttribute)}";
        public const string VARIABLE = $"QueryByShape.{nameof(VariableAttribute)}";
        public const string ARGUMENT = $"QueryByShape.{nameof(ArgumentAttribute)}";
        public const string ALIAS_OF = $"QueryByShape.{nameof(AliasOfAttribute)}";
        public const string ON = $"QueryByShape.{nameof(OnAttribute)}";
    }
}
