using QueryByShape;

namespace StarWars
{
    // Fields are excluded by default
    [Query(OperationName = "SimpleIsh", IncludeFields = true)]
    public partial class SimpleQuery : IGeneratedQuery
    {
        public CountModel AllPeople { get; set; }
    }

    public class CountModel
    {
        public int TotalCount;
    }
}