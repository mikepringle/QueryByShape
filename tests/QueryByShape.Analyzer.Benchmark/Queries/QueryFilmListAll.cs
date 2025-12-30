using System.Text.Json.Serialization;

namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    public partial class QueryFilmListAll_i : IGeneratedQuery
    {
        [JsonPropertyName("AllFilms")]
        public AllFilmSummary_i Films { get; set; }
    }
}
