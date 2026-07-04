# AGENTS.md

This file provides guidance to AI coding agents when working with code in this repository.

## Commands

```sh
# Build
dotnet build QueryByShape.sln

# Run all tests
dotnet test QueryByShape.sln

# Run a single test project
dotnet test tests/QueryByShape.Analyzer.Tests/

# Run tests with a filter
dotnet test --filter "FullyQualifiedName~QueryByShape.Analyzer.Tests.SourceGenerator"

# Pack for NuGet
dotnet pack --configuration Release

# Run benchmarks
dotnet run --project tests/QueryByShape.Analyzer.Benchmark --configuration Release
```

Snapshot tests use [Verify](https://github.com/VerifyTests/Verify). When generator output changes intentionally, run tests with `VERIFY_AUTO_ACCEPT=true` or use the `AcceptAllChanges` setting to update the `.verified.txt` files in `tests/QueryByShape.Analyzer.Tests/`.

## Architecture

**QueryByShape** is a Roslyn incremental source generator that emits GraphQL query strings at build time based on the shape of annotated C# types. It is distributed as a NuGet package. The solution has four projects:

### `QueryByShape.Attributes` (netstandard2.0)
Public attribute types that consumers annotate their query classes with: `[Query]`, `[Variable]`, `[Argument]`, `[AliasOf]`, `[On]`. Targets netstandard2.0 for maximum compatibility.

### `QueryByShape.Analyzer` (netstandard2.0)
The source generator and analyzer. Key types:

- **`QueryGenerator`** — `IIncrementalGenerator` entry point. Finds `[Query]`-annotated partial classes/records and drives the pipeline.
- **`QueryParser`** — Walks the Roslyn symbol tree to extract `QueryMetadata` from each annotated type (fields, variables, arguments, aliases, inline fragments).
- **`QueryEmitter`** — Translates `QueryMetadata` into a GraphQL query string.
- **`QueryTemplate`** — Wraps the emitted query string in the generated C# source (implements the `static abstract ToGraphQLQuery()` method).
- **Analyzers/** — `DiagnosticAnalyzer` subclasses that validate usage and emit `QBSHAPE00X` diagnostics at design time.

The generation pipeline: `[Query]` class → `QueryParser` → `QueryMetadata` → `QueryEmitter` (GraphQL string) + `QueryTemplate` (C# wrapper) → `.g.cs` file added to compilation.

### `QueryByShape` (net8.0)
Thin core library. Defines the `IGeneratedQuery` interface, which generated types implement. This interface exposes the `static abstract ToGraphQLQuery()` method that the client extension calls.

### `QueryByShape.GraphQLClient` (net8.0)
The publicly consumed NuGet package. Contains `GraphQLClientExtensions.SendQueryByAsync<T>()`, an extension method on `IGraphQLClient` that calls `T.ToGraphQLQuery()` and dispatches the request.

### Consumer workflow
1. Consumer references `QueryByShape.GraphQLClient` (which pulls in `QueryByShape.Attributes` and the analyzer).
2. Consumer declares a `partial record MyQuery : IGeneratedQuery` annotated with `[Query]` and properties annotated with `[Variable]`/`[Argument]`/etc.
3. At build time, `QueryGenerator` emits `MyQuery.Query.g.cs` containing the GraphQL query string and the `ToGraphQLQuery()` implementation.
4. Consumer calls `graphQLClient.SendQueryByAsync<MyQuery>(new MyQuery { ... })`.

### Snapshot tests
Tests in `tests/QueryByShape.Analyzer.Tests/SourceGenerator/` exercise the full generator pipeline and snapshot the generated `.g.cs` output. Tests in `tests/QueryByShape.Analyzer.Tests/Analyzers/` verify that diagnostic errors are raised correctly. Both use XUnit + Verify.SourceGenerators.
