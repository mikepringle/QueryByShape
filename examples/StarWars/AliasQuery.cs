using QueryByShape;
using System.Collections.Generic;

namespace StarWars
{
    [Query]
    [Variable("$newHopeId", "ID!")]
    [Variable("$empireId", "ID!")]
    public partial class AliasQuery : IGeneratedQuery
    {
        [Argument("id", "$newHopeId")]
        [AliasOf("film")]
        public FilmDirectorModel NewHope { get; set; }

        [Argument("id", "$empireId")]
        [AliasOf("film")]
        public FilmDirectorModel Empire { get; set; }
    }

    public class FilmDirectorModel
    {
        public string Director { get; set; }
    }
}