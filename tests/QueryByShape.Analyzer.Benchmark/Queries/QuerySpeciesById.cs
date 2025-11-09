namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$speciesID", "ID!")]
    public partial class QuerySpeciesById : IGeneratedQuery
    {
        [Argument("speciesID", "$speciesID")]
        public SpeciesSummary Starship { get; set; }
    }
}
