using VerifyXunit;
using Xunit;
using QueryByShape;
using QueryByShape.Analyzer.Diagnostics;

namespace QueryByShape.Analyzer.Tests;

public class DiagnosticTests
{
    [Fact]
    public void GeneratesPartialClassDiagnostic()
    {
        var source = @"
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query]
                public class NameQuery : IGeneratedQuery
                {
                    public List<Person> People { get; set; }
                }

                public class Person
                {
                    public string Name { get; set; }
                }
            }
        ";

        TestHelper.VerifyDiagnostic(source, QueryMustBePartialDiagnostic.Descriptor);
    }

    [Fact]
    public void GeneratesDuplicateVariableDiagnostic()
    {
        var source = @"
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query]
                [Variable(""$id"", ""UUID!"")]
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

        TestHelper.VerifyDiagnostic(source, DuplicateVariableDiagnostic.Descriptor);
    }

    [Fact]
    public void GeneratesDuplicateArgumentDiagnostic()
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

        TestHelper.VerifyDiagnostic(source, DuplicateArgumentDiagnostic.Descriptor);
    }

    [Fact]
    public void GeneratesMissingVariableDiagnostic()
    {
        var source = @"
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query]
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

        TestHelper.VerifyDiagnostic(source, MissingVariableDiagnostic.Descriptor);
    }

    [Fact]
    public void GeneratesSharedMissingVariableDiagnostic()
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

        TestHelper.VerifyDiagnostic(source, MissingVariableDiagnostic.Descriptor);
    }

    [Fact]
    public void GeneratesUnsusedVariableDiagnostic()
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

        TestHelper.VerifyDiagnostic(source, UnusedVariableDiagnostic.Descriptor);
    }

    [Fact]
    public void GeneratesSharedUnsusedVariableDiagnostic()
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
                [Variable(""$unused"", ""UUID!"")]
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

        TestHelper.VerifyDiagnostic(source, UnusedVariableDiagnostic.Descriptor);
    }

    [Fact]
    public void DoesntGeneratesDiagnosticsForKitchenSinkQuery()
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

        TestHelper.GetGeneratorResult(source, out var diagnostics);
        Assert.Empty(diagnostics);
    }
}