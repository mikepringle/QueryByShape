# AGENTS.md

Guidance for AI coding agents helping a consumer use the `QueryByShape.GraphQLClient` NuGet package.

## What this package does

QueryByShape is a Roslyn source generator. A consumer defines a `partial` class/record annotated with `[Query]` that mirrors the shape of the GraphQL response they want, and at build time the generator emits the corresponding GraphQL query string plus a `ToGraphQLQuery()` implementation. The consumer never writes the GraphQL query text by hand — it is inferred from the C# type shape and attributes.

## Requirements

- .NET 8 or higher.
- The query type must be declared `partial` (the generator adds a second partial file with the generated members).
- The query type must implement `IGeneratedQuery`.
- Serialization must use `System.Text.Json` / `GraphQL.Client.Serializer.SystemTextJson`.
- Variables and arguments must be declared ahead of time via attributes — they cannot be added dynamically at runtime.
- Dictionaries are not supported as property types.

## Attributes

- `[Query(OperationName = "...", IncludeFields = true)]` — marks the class as a generated query. Fields (not just properties) are excluded from the emitted query unless `IncludeFields` is set.
- `[Variable("$name", "Type!")]` — declares a GraphQL variable on the operation. Applied at the class level.
- `[Argument("argName", "$variableName")]` — applied to a property, passes a variable as an argument to that field.
- `[AliasOf("fieldName")]` — applied to a property, aliases it to a different underlying GraphQL field name (useful when querying the same field twice with different arguments).
- `[On("TypeName")]` — applied to a property, emits it inside an inline fragment (`... on TypeName { ... }`) for GraphQL interface/union fields.
- Standard `System.Text.Json` attributes are respected: `[JsonIgnore]` excludes a property from the query, `[JsonPropertyName("name")]` renames the emitted field.

## Basic usage pattern

```csharp
using QueryByShape;

[Query]
[Variable("$id", "ID!")]
public partial class PersonQuery : IGeneratedQuery
{
    [Argument("id", "$id")]
    public PersonModel Person { get; set; }
}

public class PersonModel
{
    public string Id { get; set; }
    public string Name { get; set; }
}
```

```csharp
GraphQLResponse<PersonQuery> response = await client.SendQueryByAsync<PersonQuery>(variables);
PersonQuery result = response?.Data;
```

`SendQueryByAsync<T>()` is an extension method on `IGraphQLClient` (from `GraphQL.Client.Abstractions`). It calls the generated `T.ToGraphQLQuery()` and dispatches the request — there is no separate query-building step.

## When helping a consumer

- Do not hand-write GraphQL query strings for these types — the whole point of the library is that the query is derived from the C# shape. If the desired query can't be expressed by adjusting the type shape/attributes, that's a sign the feature may be unsupported (e.g. dictionaries, dynamic variables).
- If a generated query looks wrong, check the type's properties/attributes first, not the generator internals — those live in the package, not the consumer's project.
- Build errors referencing `QBSHAPE00X` diagnostics come from the packaged analyzer and indicate a usage problem (e.g. missing `partial`, missing variable declaration) — read the diagnostic message, it names the specific rule violated.
- After changing a query type's shape or attributes, a rebuild is required to regenerate the query string; there is no runtime reflection fallback.

See the [README](https://www.nuget.org/packages/QueryByShape.GraphQLClient) for more complete examples (variables/arguments, aliasing, inline fragments).
