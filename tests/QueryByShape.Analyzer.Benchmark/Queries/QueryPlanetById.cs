using QueryByShape;
using QueryByShape.Analyzer.Benchmark.Queries;
using System.Text.Json.Serialization;

namespace QueryByShape.Analyzer.Benchmark.Queries
{
    [Query]
    [Variable("$planetID", "ID!")]
    public partial class QueryPlanetById : IGeneratedQuery
    {
        [Argument("planetID", "$planetID")]
        public PlanetSummary Planet { get; set; }

        [JsonIgnore]
        public string IgnoredProperty { get; set; }

    }
}
