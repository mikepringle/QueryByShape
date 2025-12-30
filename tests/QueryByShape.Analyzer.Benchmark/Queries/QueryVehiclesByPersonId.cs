namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$personID", "ID!")]
    public partial class QueryVehiclesByPersonId_i : IGeneratedQuery
    {
        [Argument("personID", "$personID")]
        public PersonVehicles_i Person { get; set; }
    }
}
