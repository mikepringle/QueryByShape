namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$planetID", "ID!")]
    public partial class QueryResidentsByPlanetId : IGeneratedQuery
    {
        [Argument("planetID", "$planetID")]
        public PlanetResidentsConnection Planet { get; set; }
    }
}