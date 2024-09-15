using GraphQL;
using GraphQL.Client.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QueryByShape.GraphQLClient
{
    public static class GraphQLClientExtensions
    {
        public static Task<GraphQLResponse<T>> SendQueryByAsync<T>(this IGraphQLClient client, object? variables = null, CancellationToken cancellationToken = default) where T: IGeneratedQuery 
        {
            var request = T.ToGraphQLQuery() ?? throw new ArgumentNullException("GeneratedQuery is null");
            return client.SendQueryAsync<T>(query:request, variables: variables, cancellationToken: cancellationToken);
        }
    }
}