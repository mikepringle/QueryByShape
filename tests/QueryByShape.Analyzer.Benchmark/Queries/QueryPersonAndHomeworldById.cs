namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$personID", "ID!")]
    public partial class QueryPersonAndHomeworldById : IGeneratedQuery
    {
        [Argument("personID", "$personID")]
        public PersonWithHomeworld Person { get; set; }
    }
}
