using VerifyXunit;
using Xunit;
using QueryByShape.Analyzer.Diagnostics;
using QueryByShape.Analyzer.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using System.Text.Json.Serialization;

namespace QueryByShape.Analyzer.Tests.DiagnosticAnalyzers;

public class QueryAnalyzerTests
{
    [Fact]
    public async Task InvalidOperationNameDiagnosticTest()
    {
        var source = @"
            using System;
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [{|#0:Query(OperationName = ""2_2"")|}]
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

        await AnalyzerTest<QueryAnalyzer>.VerifyAsync(
            source,
            new DiagnosticResult(InvalidOperationNameDiagnostic.Descriptor)
                .WithLocation(0)
                .WithArguments("2_2", "First character of name may not be numeric")
        );
    }
}