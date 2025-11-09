namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$personID", "ID!")]
    public partial class QueryVitalByPersonId : IGeneratedQuery
    {
        [Argument("personID", "$personID")]
        public PersonVitals Person { get; set; }
    }
}