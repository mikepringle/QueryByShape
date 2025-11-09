namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$filmID", "ID!")]
    public partial class QueryFilmById : IGeneratedQuery
    {
        [Argument("filmID", "$filmID")]
        public FilmSummary Film { get; set; }
    }
}
