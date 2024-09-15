using QueryByShape;
using System.Collections.Generic;

namespace StarWars
{
    [Query]
    [Variable("$id", "ID!")]
    public partial class VariablesAndArgumentsQuery : IGeneratedQuery
    {
        [Argument("id", "$id")]
        public PersonModel Person { get; set; }
    }

    public class PersonModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public HomeworldNameModel Homeworld { get; set; }
    }

    public class HomeworldNameModel
    {
        public string Name { get; set; }
    }
}