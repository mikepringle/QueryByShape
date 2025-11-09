namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$filmID", "ID!")]
    public partial class QueryFilmCharacters : IGeneratedQuery
    {
        [Argument("filmID", "$filmID")]
        public FilmCharacterConnection Film { get; set; }
    }
}
