using QueryByShape.Analyzer.Diagnostics;
using QueryByShape.Analyzer.Analyzers;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using System.ComponentModel;

namespace QueryByShape.Analyzer.Tests.DiagnosticAnalyzers;

public class ArgumentAnalyzerTests
{
    [Fact]
    public async Task InvalidArgumentNameDiagnosticTest()
    {
        var source = @"
            using System;
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query]
                [Variable(""$id"", ""UUID!"")]
                public partial class NameQuery : IGeneratedQuery
                {
                    [{|#0:Argument(""1id"", ""$id"")|}]
                    [{|#1:Argument(""i-d"", ""$id"")|}]
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

        await AnalyzerTest<ArgumentAnalyzer>.VerifyAsync(
            source,
            new DiagnosticResult(InvalidArgumentNameDiagnostic.Descriptor)
                .WithLocation(0)
                .WithArguments("1id", "First character of name may not be numeric"),
            
            new DiagnosticResult(InvalidArgumentNameDiagnostic.Descriptor)
                .WithLocation(1)
                .WithArguments("i-d", "Must only contain alphanumeric characters or undercores")
        );
    }

    [Fact]
    public async Task DuplicateArgumentNameDiagnosticTest()
    {
        var source = @"
            using System;
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query]
                [Variable(""$id"", ""UUID!"")]
                public partial class NameQuery : IGeneratedQuery
                {
                    [Argument(""id"", ""$id"")]
                    [{|#0:Argument(""id"", ""$id"")|}]
                    [{|#1:Argument(""id"", ""$id"")|}]
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

        await AnalyzerTest<ArgumentAnalyzer>.VerifyAsync(
            source,
            new DiagnosticResult(DuplicateArgumentDiagnostic.Descriptor)
                .WithLocation(0)
                .WithArguments("id"),

            new DiagnosticResult(DuplicateArgumentDiagnostic.Descriptor)
                .WithLocation(1)
                .WithArguments("id")
        );
    }
}