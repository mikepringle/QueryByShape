using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using QueryByShape.Analyzer.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace QueryByShape.Analyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class QueryDeclarationAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [QueryMustImplementDiagnostic.Descriptor, QueryMustBePartialDiagnostic.Descriptor];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
            
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not TypeDeclarationSyntax typeSyntax)
            {
                return;
            }

            var attributeNamedType = context.Compilation.ResolveNamedType<QueryAttribute>();
            var symbol = context.ContainingSymbol as INamedTypeSymbol;
            var attributes = symbol!.GetAttributes();
            var isQuery = attributes.Any(a => a.AttributeClass?.Equals(attributeNamedType, SymbolEqualityComparer.Default) == true);

            if (isQuery == false)
            {
                return;
            }

            ReportNotImplementing(symbol, context);
            ReportNotPartial(typeSyntax, symbol, context);
        }

        private static void ReportNotImplementing(INamedTypeSymbol symbol, SyntaxNodeAnalysisContext context)
        {
            var interfaceType = context.Compilation.GetTypeByMetadataName("QueryByShape.IGeneratedQuery")!;
            
            if (interfaceType.IsAssignableFrom(symbol) == false)
            {
                context.ReportDiagnostic(
                    QueryMustImplementDiagnostic.Create(symbol.Name, symbol.Locations[0])
                );
            }
        }

        private static void ReportNotPartial(TypeDeclarationSyntax typeSyntax, INamedTypeSymbol symbol, SyntaxNodeAnalysisContext context)
        {
            if (typeSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)) == false)
            {
                context.ReportDiagnostic(
                    QueryMustBePartialDiagnostic.Create(symbol.Name, symbol.Locations[0])
                );
            }
        }
    }
}