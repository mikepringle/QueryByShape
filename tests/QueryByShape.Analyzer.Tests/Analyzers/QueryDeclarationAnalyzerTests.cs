using VerifyXunit;
using Xunit;
using QueryByShape.Analyzer.Diagnostics;
using QueryByShape.Analyzer.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using System.Text.Json.Serialization;

namespace QueryByShape.Analyzer.Tests.DiagnosticAnalyzers;

public class QueryDeclarationAnalyzerTests
{
    [Fact]
    public async Task QueryMustBePartialDiagnosticTest()
    {
        var source = @"
            using System;
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query]
                [Variable(""$id"", ""UUID!"")]
                public class {|#0:NameQuery|} : IGeneratedQuery
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

        await AnalyzerTest<QueryDeclarationAnalyzer>.VerifyAsync(
            source,
            new DiagnosticResult(QueryMustBePartialDiagnostic.Descriptor)
                .WithLocation(0)
                .WithArguments("NameQuery"),
            DiagnosticResult.CompilerError("CS0260")
                .WithLocation(0)
                .WithArguments("NameQuery")

        );
    }

    [Fact]
    public async Task QueryMustImplementDiagnosticTest()
    {
        var source = @"
            using System;
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query]
                [Variable(""$id"", ""UUID!"")]
                public partial class {|#0:NameQuery|}
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

        await AnalyzerTest<QueryDeclarationAnalyzer>.VerifyAsync(
            source,
            new DiagnosticResult(QueryMustImplementDiagnostic.Descriptor)
                .WithLocation(0)
                .WithArguments("NameQuery")
        );
    }
}