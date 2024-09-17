# QueryByShape
QueryByShape autogenerates GraphQL queries at build-time based on the "shape" of your result type.  The current / first implementation is an extension to the [GraphQL.Client (.NET)](https://github.com/graphql-dotnet/graphql-client). 

## Usage
``` C#
GraphQLResponse<StarWarsFilms> filmsResponse = await client.SendQueryByAsync<StarWarsFilms>(variables);
StarWarsFilms films = filmsResponse?.Data;
```

## Goals
- Infer / generate a GraphQL Query based on the shape of a result type
- Composing objects vs gql queries 
- Performance - generate queries at build time

## v1 Features
- [x] Queries
- [x] Variables
- [x] Arguments
- [x] Aliasing
- [x] JsonIgore (System.Text.Json)
- [x] JsonPropertyName (System.Text.Json)
- [ ] Default Variables
- [ ] Interfaces
- [ ] API Documentation
- [ ] Unions (possibly v1.5)

## Limitations
- Requires .NET 8 or higher
- Only supports System.Text.Json / GraphQL.Client.Serializer.SystemTextJson
- Queries must be partial
- Queries must define variables / arguments ahead of time
- Dictionaries are not supprted

## Installation
This project is currently in beta you will need to [enable prerelease packages](https://learn.microsoft.com/en-us/nuget/create-packages/prerelease-packages) in UI or cli    
`dotnet add package QueryByShape.GraphQLClient --prerelease `  


## Example Queries
All samples are written for the [Star Wars GraphQL API](https://studio.apollographql.com/public/star-wars-swapi)


<table>
<tr>
  <td> Query Options - C# </td> 
</tr>
<tr>
<td>

``` C#
using QueryByShape;

namespace StarWars
{
    // Fields are excluded by default
    [Query(OperationName="SimpleIsh", IncludeFields=true)]
    public partial class SimpleQuery : IGeneratedQuery
    {
        public CountModel AllPeople { get; set; }
    }

    public class CountModel
    {
        public int TotalCount; 
    }
}
```
</td>
</tr>
<tr>
<td> GraphQL Output </td>
</tr>
<tr>
<td valign="top">

``` GraphQL
query SimpleIsh {
  allPeople {
    totalCount
  }
}
```

</td>
</tr>
</table>


<table>
<tr>
<td> Variables and Arguments - C# </td>
</tr>
<tr>
<td>

``` C# 
using QueryByShape;
using System.Collections.Generic;

namespace StarWars
{
    [Query]
    [Variable("$id", "ID!")]
    public partial class VariablesAndArgumentsQuery : IGeneratedQuery
    {
        [Argument("id", "$id")]
        public PersonModel Person { get; set; }
    }

    public class PersonModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public HomeworldNameModel Homeworld { get; set; }
    }

    public class HomeworldNameModel
    {
        public string Name { get; set; }
    }
}
```

</td>
</tr>
<tr>
<td> GraphQL Output </td>
</tr>
<tr>

<td valign="top">

``` GraphQL
query VariablesAndArgumentsQuery($id: ID!) {
  person (id: $id) {
     id
     name
     homeworld {
      name
    } 
  }
}
```

</td>
</tr>
</table>

<table>
<tr>
<td> Aliasing - C# </td>
</tr>
<tr>
<td>

``` C# 
using QueryByShape;
using System.Collections.Generic;

namespace StarWars
{
    [Query]
    [Variable("$newHopeId", "ID!")]
    [Variable("$empireId", "ID!")]
    public partial class AliasQuery : IGeneratedQuery
    {
        [Argument("id", "$newHopeId")]
        [AliasOf("film")]
        public FilmDirectorModel NewHope { get; set; }

        [Argument("id", "$empireId")]
        [AliasOf("film")]
        public FilmDirectorModel Empire { get; set; }
    }

    public class FilmDirectorModel
    {
        public string Director { get; set; }
    }
}
```

</td>
</tr>
<tr>
<td> GraphQL Output </td>
</tr>
<tr>

<td valign="top">

``` GraphQL
query ExampleQuery ($newHopeId: ID!, $empireId: ID!) {
  newHope: film (id: $newHopeId ) {
    director
  }
  empire: film (id: $empireId ) {
    director
  }
}

```

</td>
</tr>
</table>

<table>
<tr>
<td> Supported System.Text.Json Attributes - C# </td>
</tr>
<tr>
<td>

``` C# 
using QueryByShape;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StarWars
{
    [Query]
    [Variable("$id", "ID!")]
    public partial class SystemTextJsonAttributesQuery : IGeneratedQuery
    {
        [Argument("id", "$id")]
        public PersonHeightModel Person { get; set; }
    }

    public class PersonHeightModel
    {
        public string Id { get; set; }
        [JsonIgnore]
        public Guid TempId { get; set; }
        [JsonPropertyName("name")]
        public string PersonName { get; set; }
        public int Height  { get; set; }
    }
}
```

</td>
</tr>
<tr>
<td> GraphQL Output </td>
</tr>
<tr>

<td valign="top">

``` GraphQL
query SystemTextJsonAttributes($id: ID!) {
  person (id: $id) {
     id
     name 
     height
  }
}

```

</td>
</tr>
</table>
