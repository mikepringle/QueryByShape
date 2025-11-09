namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$filmID", "ID!")]
    public partial class QueryStarshipsByFilmId : IGeneratedQuery
    {
        [Argument("filmID", "$filmID")]
        public FilmStarships Film { get; set; }
    }
}