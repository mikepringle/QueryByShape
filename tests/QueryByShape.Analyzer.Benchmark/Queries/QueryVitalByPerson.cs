namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$personID", "ID!")]
    public partial class QueryVitalByPersonId_i : IGeneratedQuery
    {
        [Argument("personID", "$personID")]
        public PersonVitals_i Person { get; set; }
    }
}