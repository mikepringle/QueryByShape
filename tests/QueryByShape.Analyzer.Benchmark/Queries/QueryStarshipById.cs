namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$starshipID", "ID!")]
    public partial class QueryStarshipById : IGeneratedQuery
    {
        [Argument("starshipID", "$starshipID")]
        public StarshipSummary Starship { get; set; }
    }
}
