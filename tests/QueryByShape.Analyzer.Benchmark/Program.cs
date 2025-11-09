using BenchmarkDotNet.Running;
using QueryByShape.Analyzer.Benchmark;

var summary = BenchmarkRunner.Run<SourceGeneratorBenchmark>();