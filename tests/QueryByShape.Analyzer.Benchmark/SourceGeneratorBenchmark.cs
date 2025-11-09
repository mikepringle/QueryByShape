using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace QueryByShape.Analyzer.Benchmark
{
    [MemoryDiagnoser]
    public class SourceGeneratorBenchmark
    {
        private SyntaxTree[] _syntaxTrees;
        private MetadataReference[] _references;
        private CSharpCompilation _compilation;

        [GlobalSetup]
        public void GlobalSetup()
        {
            string baseDirectory = AppContext.BaseDirectory;
            Console.WriteLine($"Base Directory: {baseDirectory}");  
            string directory = Path.GetFullPath(Path.Combine(baseDirectory, @"..\..\..\..\..\..\..\Queries"));
            Console.WriteLine($"Loading source files from: {directory}");
            var trees = new List<SyntaxTree>();

            foreach (string file in Directory.EnumerateFiles(directory))
            {
                string fileSource = File.ReadAllText(file);
                trees.Add(CSharpSyntaxTree.ParseText(fileSource));
            }
            
            _syntaxTrees = trees.ToArray();


            _references = AppDomain.CurrentDomain.GetAssemblies()
                      .Where(assembly => !assembly.IsDynamic)
                      .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
                      .Cast<MetadataReference>()
                      .Concat(new[] {
                                MetadataReference.CreateFromFile(typeof(IGeneratedQuery).Assembly.Location),
                                MetadataReference.CreateFromFile(typeof(QueryAttribute).Assembly.Location),
                                MetadataReference.CreateFromFile(typeof(JsonIgnoreAttribute).Assembly.Location),
                      }).ToArray();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            _compilation = CSharpCompilation.Create("SourceGeneratorTests",
                          _syntaxTrees,
                          _references,
                          new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));                       
        }

        [Benchmark]
        public void RunGenerator()
        {
            var generator = new QueryGenerator();

            GeneratorDriver driver = CSharpGeneratorDriver
                                        .Create(generator)
                                        .RunGenerators(_compilation);
            
            //var results = driver.GetRunResult().Results.SelectMany(x => x.GeneratedSources).Select(x => x.SourceText.ToString()).ToArray();
            //return string.Join('\n', results);
        }
    }
}
