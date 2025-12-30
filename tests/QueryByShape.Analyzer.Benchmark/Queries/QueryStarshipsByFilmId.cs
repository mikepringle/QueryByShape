namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$filmID", "ID!")]
    public partial class QueryStarshipsByFilmId_i : IGeneratedQuery
    {
        [Argument("filmID", "$filmID")]
        public FilmStarships_i Film { get; set; }
    }
}