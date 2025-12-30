namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$speciesID", "ID!")]
    public partial class QuerySpeciesById_i : IGeneratedQuery
    {
        [Argument("speciesID", "$speciesID")]
        public SpeciesSummary Starship { get; set; }
    }
}
