namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$person1ID", "ID!")]
    [Variable("$person2ID", "ID!")]
    public partial class QueryComparePeople : IGeneratedQuery
    {
        [Argument("person1ID", "$person1ID")]
        [AliasOf("Person")]
        public PersonDetails PersonOne { get; set; }

        [Argument("person2ID", "$person2ID")]
        [AliasOf("Person")]
        public PersonDetails PersonTwo { get; set; }
    }
}
