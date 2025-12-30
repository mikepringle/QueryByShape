namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$filmID", "ID!")]
    public partial class QueryFilmById_i : IGeneratedQuery
    {
        [Argument("filmID", "$filmID")]
        public FilmSummary_i Film { get; set; }
    }
}
