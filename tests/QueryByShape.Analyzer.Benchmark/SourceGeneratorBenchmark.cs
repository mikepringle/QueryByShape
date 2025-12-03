using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text.Json.Serialization;

namespace QueryByShape.Analyzer.Benchmark
{
    [MemoryDiagnoser]
    public class SourceGeneratorBenchmark
    {
        private SyntaxTree[] _syntaxTrees;
        private MetadataReference[] _references;
        private CSharpCompilation _compilation;
        private CSharpCompilation[] _rerunCompilations;

        [Params(0, 5, 10)]
        public int RerunTimes;

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

            _rerunCompilations = new CSharpCompilation[RerunTimes];

            for (var i = 0; i < RerunTimes; i++)
            {
                _rerunCompilations[i] = _compilation.Clone();
            }
        }

        [Benchmark]
        public void RunGenerator()
        {
            var generator = new QueryGenerator().AsSourceGenerator();

            var opts = new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true);

            GeneratorDriver driver = CSharpGeneratorDriver.Create([generator], driverOptions: opts);
            driver = driver.RunGenerators(_compilation);
            GeneratorDriverRunResult runResult = driver.GetRunResult();

            for (var i = 0; i < _rerunCompilations.Length; i++)
            {
                GeneratorDriverRunResult rerunResult = driver.RunGenerators(_rerunCompilations[i]).GetRunResult();
                var cached = rerunResult
                    .Results[0]
                    .TrackedOutputSteps
                    .Where(x => x.Value.SelectMany(y => y.Outputs).Any(z => z.Reason == IncrementalStepRunReason.Cached))
                    .Select(x => x.Key)
                    .ToArray();

                Console.WriteLine($"Rerun Cached: " + (cached.Length == 0 ? "None" : String.Join(',', cached)));
            }
        }
    }
}
