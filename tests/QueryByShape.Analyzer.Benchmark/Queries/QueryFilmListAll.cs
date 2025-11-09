using System.Text.Json.Serialization;

namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    public partial class QueryFilmListAll : IGeneratedQuery
    {
        [JsonPropertyName("AllFilms")]
        public AllFilmSummary Films { get; set; }
    }
}
