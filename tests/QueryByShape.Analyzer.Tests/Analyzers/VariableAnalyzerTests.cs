using QueryByShape.Analyzer.Diagnostics;
using QueryByShape.Analyzer.Analyzers;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using System.ComponentModel;

namespace QueryByShape.Analyzer.Tests.DiagnosticAnalyzers;

public class VariableAnalyzerTests
{
    [Fact]
    public async Task InvalidVariableNameDiagnosticTest()
    {
        var source = @"
            using System;
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query]
                [Variable(""$id"", ""UUID!"")]
                [{|#0:Variable(""id"", ""UUID!"")|}]
                [{|#1:Variable(""$4id"", ""UUID!"")|}]
                [{|#2:Variable(""$i%d"", ""UUID!"")|}]
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

        await AnalyzerTest<VariableAnalyzer>.VerifyAsync(
            source,
            new DiagnosticResult(InvalidVariableNameDiagnostic.Descriptor)
                .WithLocation(0)
                .WithArguments("id", "Must start with $"),

            new DiagnosticResult(InvalidVariableNameDiagnostic.Descriptor)
                .WithLocation(1)
                .WithArguments("$4id", "First character of name may not be numeric"),

            new DiagnosticResult(InvalidVariableNameDiagnostic.Descriptor)
                .WithLocation(2)
                .WithArguments("$i%d", "Must only contain alphanumeric characters or undercores")
        );
    }

    [Fact]
    public async Task DuplicateVariableNameDiagnosticTest()
    {
        var source = @"
            using System;
            using QueryByShape;
            using System.Collections.Generic;

            namespace Tests
            {
                [Query]
                [Variable(""$id"", ""UUID!"")]
                [{|#0:Variable(""$id"", ""UUID!"")|}]
                [{|#1:Variable(""$id"", ""UUID!"")|}]
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

        await AnalyzerTest<VariableAnalyzer>.VerifyAsync(
            source,
            new DiagnosticResult(DuplicateVariableDiagnostic.Descriptor)
                .WithLocation(0)
                .WithArguments("$id"),

            new DiagnosticResult(DuplicateVariableDiagnostic.Descriptor)
                .WithLocation(1)
                .WithArguments("$id")
        );
    }
}