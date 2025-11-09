using System.Numerics;

namespace QueryByShape.Analyzer.Benchmark.Queries
{
    public class PersonDetails
    {
        public DateTime BirthYear { get; set; }
        
        public string Name { get; set; }

        public DateTime Created {  get; set; }

        public DateTime Edited { get; set; }

        public string EyeColor { get; set; }    

        public string Gender { get; set; }

        public string HairColor { get; set; }

        public int Height { get; set; }

        public PlanetSummary Planet { get; set; }

        public decimal Mass { get; set; }

        public string SkinColor { get; set; }

        public string Species { get; set; }
    }
}
