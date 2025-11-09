namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$personID", "ID!")]
    public partial class QueryVehiclesByPersonId : IGeneratedQuery
    {
        [Argument("personID", "$personID")]
        public PersonVehicles Person { get; set; }
    }
}
