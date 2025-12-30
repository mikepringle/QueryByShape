namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$planetID", "ID!")]
    public partial class QueryResidentsByPlanetId_i : IGeneratedQuery
    {
        [Argument("planetID", "$planetID")]
        public PlanetResidentsConnection_i Planet { get; set; }
    }
}