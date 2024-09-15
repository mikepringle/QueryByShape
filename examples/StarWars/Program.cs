using GraphQL.Client.Http;
using GraphQL;
using System.Text.Json;
using GraphQL.Client.Serializer.SystemTextJson;
using QueryByShape.GraphQLClient;
using StarWars;
using GraphQL.Client.Abstractions;
using System.Collections.Generic;

var serializerOptions = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
using var clientWithFields = new GraphQLHttpClient("https://swapi-graphql.netlify.app/.netlify/functions/index", new SystemTextJsonSerializer(serializerOptions));

Console.WriteLine(SimpleQuery.ToGraphQLQuery());
var simpleResponse = await clientWithFields.SendQueryByAsync<SimpleQuery>();
Console.WriteLine("raw response:");
Console.WriteLine(JsonSerializer.Serialize(simpleResponse.Data, serializerOptions));
Console.WriteLine();

clientWithFields.Dispose();

serializerOptions = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
using var client = new GraphQLHttpClient("https://swapi-graphql.netlify.app/.netlify/functions/index", new SystemTextJsonSerializer(serializerOptions));

Console.WriteLine(VariablesAndArgumentsQuery.ToGraphQLQuery());
var chewbaccaResponse = await client.SendQueryByAsync<VariablesAndArgumentsQuery>(new { id= "cGVvcGxlOjEz" });
Console.WriteLine("raw response:");
Console.WriteLine(JsonSerializer.Serialize(chewbaccaResponse.Data, serializerOptions));
Console.WriteLine();

Console.WriteLine(AliasQuery.ToGraphQLQuery());
var directorsResponse = await client.SendQueryByAsync<AliasQuery>(new { newHopeId = "ZmlsbXM6MQ==", empireId= "ZmlsbXM6Mg==" });
Console.WriteLine("raw response:");
Console.WriteLine(JsonSerializer.Serialize(directorsResponse.Data, serializerOptions));
Console.WriteLine();

Console.WriteLine(SystemTextJsonAttributesQuery.ToGraphQLQuery());
var vaderResponse = await client.SendQueryByAsync<SystemTextJsonAttributesQuery>(new Dictionary<string , string>() { { "id", "cGVvcGxlOjQ=" } } );
Console.WriteLine("raw response:");
Console.WriteLine(JsonSerializer.Serialize(vaderResponse.Data, serializerOptions));
Console.WriteLine();

Console.WriteLine("Press any key to quit...");
Console.ReadKey();

