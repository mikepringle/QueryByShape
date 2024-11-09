using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Resources;
using System.Text.Json.Serialization;

namespace QueryByShape.Analyzer.Tests.DiagnosticAnalyzers
{
    public class AnalyzerTest<T> : CSharpAnalyzerTest<T, DefaultVerifier> where T : DiagnosticAnalyzer, new()
    {
        const string SOURCE_GEN = @"#nullable enable annotations
            #nullable disable warnings

            // Suppress warnings about [Obsolete] member usage in generated code.
            // #pragma warning disable CS0612, CS0618

            namespace Tests
            { 
                public partial class NameQuery
                {
                    public static string? ToGraphQLQuery() => null;                 
                }   
            }
        ";

        public AnalyzerTest(string source, params DiagnosticResult[] expected)
        {
            CompilerDiagnostics = CompilerDiagnostics.Errors;
            ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
            ExpectedDiagnostics.AddRange(expected);

            TestState.Sources.Add(("Test.cs", source));
            TestState.Sources.Add(("SourceGen.g.cs", SOURCE_GEN));
            TestState.AdditionalReferences.Add(typeof(QueryAttribute).Assembly.Location);
            TestState.AdditionalReferences.Add(typeof(IGeneratedQuery).Assembly.Location);
            TestState.AdditionalReferences.Add(typeof(JsonIgnoreAttribute).Assembly.Location);
        }

        public static async Task VerifyAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new AnalyzerTest<T>(source, expected);
            await test.RunAsync();
        }
    }
}
