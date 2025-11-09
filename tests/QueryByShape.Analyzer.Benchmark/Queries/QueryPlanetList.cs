using System.Text.Json.Serialization;

namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    public partial class QueryPlanetList : IGeneratedQuery
    {
        [JsonPropertyName("AllPlanets")]
        public AllPlantSummary Planets { get; set; }
    }
}
