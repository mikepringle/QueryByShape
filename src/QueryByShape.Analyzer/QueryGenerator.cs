using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using QueryByShape.Analyzer.Diagnostics;

namespace QueryByShape.Analyzer
{
    [Generator]
    public class QueryGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {

#if DEBUG
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                //Debugger.Launch();
            }
#endif
            var symbols = context.CompilationProvider
                .Select((compilation, _) => new NamedTypeSymbols(compilation))
                .WithTrackingName(TrackingNames.Symbols);

            var queryDeclarationss = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeNames.QUERY,
                    (node, cancellationToken) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                    (context, cancellationToken) => (Context: (TypeDeclarationSyntax)context.TargetNode, context.SemanticModel))
                .WithTrackingName(TrackingNames.QueryDeclarations);
                                
           var queryContext = queryDeclarationss     
                .Combine(symbols)
                .Select(static (tuple, cancellationToken) =>
                {
                    var queryParser = new QueryParser(tuple.Right);
                    return queryParser.Parse(tuple.Left.Context, tuple.Left.SemanticModel, cancellationToken);
                })
                .WithTrackingName(TrackingNames.QueryHierarchy);

            context.RegisterSourceOutput(
                queryContext.Where(q => q.Diagnostics is not null).SelectMany((x, _) => x.Diagnostics.Value),
                static (context, metadata) => context.ReportDiagnostic(metadata.ToDiagnostic())
            );

            context.RegisterSourceOutput(queryContext, QueryEmitter.EmitSource);
        }
    }
}
