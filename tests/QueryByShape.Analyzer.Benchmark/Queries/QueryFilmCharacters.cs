namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$filmID", "ID!")]
    public partial class QueryFilmCharacters_i : IGeneratedQuery
    {
        [Argument("filmID", "$filmID")]
        public FilmCharacterConnection_i Film { get; set; }
    }
}
