using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace QueryByShape.Analyzer.Tests.SourceGenerator;

public static class TestHelper
{
    public static string GetGeneratorResult(string source) => GetGeneratorResult(source, out _);

    public static string GetGeneratorResult(string source, out ImmutableArray<Diagnostic> diagnostics)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain.GetAssemblies()
                            .Where(assembly => !assembly.IsDynamic)
                            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
                            .Cast<MetadataReference>()
                            .Concat(new[] {
                                MetadataReference.CreateFromFile(typeof(IGeneratedQuery).Assembly.Location),
                                MetadataReference.CreateFromFile(typeof(QueryAttribute).Assembly.Location),
                                MetadataReference.CreateFromFile(typeof(JsonIgnoreAttribute).Assembly.Location),
                            });

        var compilation = CSharpCompilation.Create("SourceGeneratorTests",
                      new[] { syntaxTree },
                      references,
                      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new QueryGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();
        diagnostics = runResult.Diagnostics;
        var results = runResult.Results.SelectMany(x => x.GeneratedSources).Select(x => x.SourceText.ToString()).ToArray();
        return string.Join('\n', results);
    }

    public static void VerifyGeneratorDiagnostic(string source, DiagnosticDescriptor expectedDescriptor)
    {
        var result = GetGeneratorResult(source, out var diagnostics);

        Assert.Single(diagnostics);
        var actualDescriptor = diagnostics[0].Descriptor;

        Assert.Equivalent(expectedDescriptor, actualDescriptor);
    }

    public static Task VerifySnapshot(string source)
    {
        return 
            Verify(GetGeneratorResult(source))
            .UseDirectory("Snapshots");
    }
}