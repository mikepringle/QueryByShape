using QueryByShape;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StarWars
{
    [Query]
    [Variable("$id", "ID!")]
    public partial class SystemTextJsonAttributesQuery : IGeneratedQuery
    {
        [Argument("id", "$id")]
        public PersonHeightModel Person { get; set; }
    }

    public class PersonHeightModel
    {
        public string Id { get; set; }
        [JsonIgnore]
        public Guid TempId { get; set; }
        [JsonPropertyName("name")]
        public string PersonName { get; set; }
        public int Height { get; set; }
    }
}