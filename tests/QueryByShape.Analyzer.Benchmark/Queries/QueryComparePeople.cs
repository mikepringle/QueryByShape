namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$person1ID", "ID!")]
    [Variable("$person2ID", "ID!")]
    public partial class QueryComparePeople_i : IGeneratedQuery
    {
        [Argument("person1ID", "$person1ID")]
        [AliasOf("Person")]
        public PersonDetails_i PersonOne { get; set; }

        [Argument("person2ID", "$person2ID")]
        [AliasOf("Person")]
        public PersonDetails_i PersonTwo { get; set; }
    }
}
