using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using QueryByShape.Analyzer.Diagnostics;
using System.Linq;
using System.Text;

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
            var _symbols = context.CompilationProvider
                .Select((compilation, _) => new NamedTypeSymbols(compilation))
                .WithTrackingName(TrackingNames.Symbols);

            var queryDeclarationss = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    AttributeNames.QUERY,
                    (node, cancellationToken) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                    (context, cancellationToken) => (Context: (TypeDeclarationSyntax)context.TargetNode, context.SemanticModel))
                .WithTrackingName(TrackingNames.QueryDeclarations);
                                
           var queryContext = queryDeclarationss     
                .Combine(_symbols)
                .Select(static (tuple, cancellationToken) =>
                {
                    return QueryParser.Parse(tuple.Left.Context, tuple.Left.SemanticModel, tuple.Right, cancellationToken);
                })
                .WithTrackingName(TrackingNames.QueryHierarchy);

            context.RegisterSourceOutput(
                queryContext.Where(q => q.Diagnostics is not null).SelectMany((x, _) => x.Diagnostics.Value),
                static (context, metadata) => context.ReportDiagnostic(metadata.ToDiagnostic())
            );

            context.RegisterSourceOutput(queryContext, static (ctx, result) => 
            {
                var (query, diagnostics) = result;

                if (diagnostics != null && diagnostics.Value.Any(d => d.Descriptor.DefaultSeverity is DiagnosticSeverity.Error))
                {
                    return;
                }

                var generatedClass = QueryTemplate.Build(query);
                ctx.AddSource($"QueryByShape.{query.NamespaceName}.{query.TypeName}.Query.g.cs", SourceText.From(generatedClass, Encoding.UTF8));
            });
        }
    }
}
