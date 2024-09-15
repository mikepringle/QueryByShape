using VerifyXunit;
using Xunit;
using QueryByShape;

namespace QueryByShape.Analyzer.Tests;

public class SnapshotTests
{
    [Fact]
    public Task GeneratesBasicQuery()
    {
        // The source code to test
        var source = @"
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query]
                public partial class NameQuery : IGeneratedQuery
                {
                    public List<Person> People { get; set; }
                }

                public class Person
                {
                    public string Name { get; set; }
                }
            }
        ";

        return TestHelper.VerifySnapshot(source);
    }

    [Fact]
    public Task GeneratesRecordQuery()
    {
        // The source code to test
        var source = @"
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query]
                public partial record NameQuery(List<Person> People) : IGeneratedQuery;
                
                public record Person(string Name) {
                    public string PreferredName { get; set; }
                }
            }
        ";

        return TestHelper.VerifySnapshot(source);
    }

    [Fact]
    public Task GeneratesWithStructQuery()
    {
        // The source code to test
        var source = @"
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query]
                public partial class NameQuery : IGeneratedQuery
                {
                    public List<Person> People { get; set; }
                }

                public struct Person
                {
                    public int Age { get; set; }
                }
            }
        ";

        return TestHelper.VerifySnapshot(source);
    }

    [Fact]
    public Task GeneratesComplexIEnumarableQuery()
    {
        // The source code to test
        var source = @"
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query]
                public partial class NameQuery : IGeneratedQuery
                {
                    public ComplexList People { get; set; }
                }

                public class Person
                {
                    public string Name { get; set; }
                }

                public class ComplexList : List<Person> 
                {
                }
            }
        ";

        return TestHelper.VerifySnapshot(source);
    }

    [Fact]
    public Task SupportsQueryName()
    {
        // The source code to test
        var source = @"
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query(OperationName = ""renamed"")]
                public partial class NameQuery : IGeneratedQuery
                {
                    public List<Person> People { get; set; }
                }

                public class Person
                {
                    public string Name { get; set; }
                }
            }
        ";

        return TestHelper.VerifySnapshot(source);
    }

    [Fact]
    public Task GeneratesFieldsWhenConfigured()
    {
        // The source code to test
        var source = @"
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query(IncludeFields = true)]
                public partial class NameQuery : IGeneratedQuery
                {
                    public List<Person> People { get; set; }
                }

                public class Person
                {
                    public string FirstName;
                    public string LastName;
                    public string MiddleName;
                }
            }
        ";

        return TestHelper.VerifySnapshot(source);
    }

    [Fact]
    public Task ExcludesFieldsWhenNotConfigured()
    {
        // The source code to test
        var source = @"
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query]
                public partial class NameQuery : IGeneratedQuery
                {
                    public List<Person> People { get; set; }
                }

                public class Person
                {
                    public string PreferredName { get; set; }
                    public string FirstName;
                    public string LastName;
                    public string MiddleName;
                }
            }
        ";

        return TestHelper.VerifySnapshot(source);
    }

    [Fact]
    public Task GeneratesAlias()
    {
        // The source code to test
        var source = @"
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query]
                public partial class NameQuery : IGeneratedQuery
                {
                    [AliasOf(""customers"")]
                    public List<Person> People { get; set; }
                }

                public class Person
                {
                    public string FirstName { get; set; }
                    public string LastName { get; set; }
                    [AliasOf(""middle"")]
                    public string MiddleName { get; set; } 
                }
            }
        ";

        return TestHelper.VerifySnapshot(source);
    }

    [Fact]
    public Task GeneratesArgumentsAndVariables()
    {
        var source = @"
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query]
                [Variable(""$id"", ""UUID!"")]
                public partial class NameQuery : IGeneratedQuery
                {
                    [Argument(""id"", ""$id"")]
                    public List<Customer> People { get; set; }
                }

                public class Customer : Person
                {
                    public Guid CustomerId { get; set; }
                }

                public class Person
                {
                    public string FirstName { get; set; }
                    public string LastName { get; set; }
                    public string MiddleName { get; set; } 
                }
            }
        ";

        return TestHelper.VerifySnapshot(source);
    }


    [Fact]
    public Task GeneratesWithJsonPropertyName()
    {
        // The source code to test
        var source = @"
            using QueryByShape;
            using System.Collections.Generic;
            using System.Text.Json.Serialization;

            namespace Tests
            {
                [Query]
                public partial class NameQuery : IGeneratedQuery
                {
                    public List<Customer> People { get; set; }
                }

                public class Customer : Person
                {
                    [JsonPropertyName(""id"")]
                    public Guid CustomerId { get; set; }
                }

                public class Person
                {
                    public string FirstName { get; set; }
                    public string LastName { get; set; }
                    [JsonPropertyName(""middle_name"")]
                    public string MiddleName { get; set; } 
                }
            }
        ";

        return TestHelper.VerifySnapshot(source);
    }

    [Fact]
    public Task GeneratesWithJsonIgnore()
    {
        var source = @"
            using QueryByShape;
            using System.Collections.Generic;
            using System.Text.Json.Serialization;

            namespace Tests
            {
                [Query]
                public partial class NameQuery : IGeneratedQuery
                {
                    public List<Customer> People { get; set; }
                }

                public class Customer : Person
                {
                    [JsonIgnore]
                    public Guid CustomerId { get; set; }
                }

                public class Person
                {
                    public string FirstName { get; set; }
                    [JsonIgnore(Condition=JsonIgnoreCondition.Never)]
                    public string LastName { get; set; }
                    [JsonIgnore(Condition=JsonIgnoreCondition.Always)]
                    public string MiddleName { get; set; } 
                }
            }
        ";

        return TestHelper.VerifySnapshot(source);
    }

    [Fact]
    public Task GeneratesKitchenSinkQuery()
    {
        var source = @"
            using QueryByShape;
            using System.Collections.Generic;
            using System.Text.Json.Serialization;

            namespace Tests
            {
                [Query(OperationName = ""renamed"", IncludeFields = true)]
                [Variable(""$id"", ""UUID!"")]
                public partial class NameQuery : IGeneratedQuery
                {
                    [Argument(""id"", ""$id"")]
                    public List<Customer> People { get; set; }
                }

                public class Customer : Person
                {
                    [JsonPropertyName(""id"")]
                    [AliasOf(""identity"")]
                    public Guid CustomerId { get; set; }

                    [JsonIgnore]
                    public string Secret { get; set; }
                }

                public class Person
                {
                    public string FirstName { get; set; }
                    [JsonIgnore(Condition=JsonIgnoreCondition.Never)]
                    public string LastName { get; set; }
                    [JsonIgnore(Condition=JsonIgnoreCondition.Always)]    
                    public string MiddleName { get; set; }
                    public string AddressLine1;
                    public string City;
                    public string State;
                    [JsonPropertyName(""zip"")]
                    public string ZipCode;
                }
            }
        ";

        return TestHelper.VerifySnapshot(source);
    }

    [Fact]
    public Task GeneratesMultipleQueriesWithDifferentOptions() {
        var source = @"
            using QueryByShape;
            using System.Collections.Generic;
            using System.Text.Json.Serialization;

            namespace Tests
            {
                [Query(IncludeFields = true)]
                [Variable(""$id"", ""UUID!"")]
                public partial class FirstQuery : IGeneratedQuery
                {
                    [Argument(""id"", ""$id"")]
                    public List<Customer> People { get; set; }    
                }

                [Query(IncludeFields = false)]
                [Variable(""$id"", ""UUID!"")]
                public partial class SecondQuery : IGeneratedQuery
                {
                    [Argument(""id"", ""$id"")]
                    public List<Customer> People { get; set; }
                }

                public class Customer : Person
                {
                    public Guid CustomerId { get; set; }
                }

                public class Person
                {
                    public string FirstName { get; set; }
                    public string LastName { get; set; }
                    public string MiddleName { get; set; }
                    public string AddressLine1;
                    public string City;
                    public string State;
                    public string ZipCode;
                }
            }
        ";

        return TestHelper.VerifySnapshot(source);
    }

    [Fact]
    public Task GeneratesMultipleQueriesWithSharedVariables()
    {
        var source = @"
            using QueryByShape;
            using System.Collections.Generic;
            using System.Text.Json.Serialization;

            namespace Tests
            {
                [Query]
                [Variable(""$id"", ""UUID!"")]
                [Variable(""$orderId"", ""UUID!"")]
                public partial class FirstQuery : IGeneratedQuery
                {
                    [Argument(""id"", ""$id"")]
                    public List<Customer> People { get; set; }    
                }

                [Query]
                [Variable(""$id"", ""UUID!"")]
                [Variable(""$orderId"", ""UUID!"")]
                public partial class SecondQuery : IGeneratedQuery
                {
                    [Argument(""id"", ""$id"")]
                    public List<Customer> People { get; set; }
                }

                public class Customer : Person
                {
                    public Guid CustomerId { get; set; }

                    [Argument(""orderId"", ""$orderId"")]
                    public List<Order> Orders { get; set; }
                }

                public class Order
                {
                    public Guid OrderId { get; set; }
                    public DateTime OrderDate { get; set; }
                }

                public class Person
                {
                    public string FirstName { get; set; }
                    public string LastName { get; set; }
                    public string MiddleName { get; set; }
                    public string AddressLine1;
                    public string City;
                    public string State;
                    public string ZipCode;
                }
            }
        ";

        return TestHelper.VerifySnapshot(source);



    }
}