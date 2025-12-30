using System.Text.Json.Serialization;

namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    public partial class QueryPlanetList_i : IGeneratedQuery
    {
        [JsonPropertyName("AllPlanets")]
        public AllPlantSummary_i Planets { get; set; }
    }
}
