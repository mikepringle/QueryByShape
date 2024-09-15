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
                .Select((compilation, _) => new NamedTypeSymbols(compilation));

            var queryContext = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeNames.QUERY,
                    (node, cancellationToken) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                    (context, cancellationToken) => (Context: (TypeDeclarationSyntax)context.TargetNode, context.SemanticModel))
                .Collect()
                .Combine(symbols)
                .SelectMany(static (tuple, cancellationToken) => QueryParser.Process(tuple.Left, tuple.Right, cancellationToken));


            context.RegisterSourceOutput(
                queryContext.SelectMany((x, _) => x.Diagnostics),
                static (context, metadata) => context.ReportDiagnostic(metadata.ToDiagnostic())
            );

            context.RegisterSourceOutput(queryContext, QueryEmitter.EmitSource);
        }
    }
}
