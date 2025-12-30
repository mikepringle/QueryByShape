namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$personID", "ID!")]
    public partial class QueryPersonAndHomeworldById_i : IGeneratedQuery
    {
        [Argument("personID", "$personID")]
        public PersonWithHomeworld_i Person { get; set; }
    }
}
