using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using QueryByShape.Analyzer.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;


namespace QueryByShape.Analyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class QueryAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [InvalidOperationNameDiagnostic.Descriptor];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.Attribute);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not AttributeSyntax attributeSyntax)
            {
                return;
            }

            var name = attributeSyntax.Name.ExtractName();

            if (name is not "Query" or nameof(QueryAttribute))
            {
                return;
            }

            var attributeNamedType = context.Compilation.ResolveNamedType<QueryAttribute>();
            var attributes = context.ContainingSymbol!.GetAttributes();
            var activeAttribute = attributes.Single(a => a.ApplicationSyntaxReference?.SyntaxTree == attributeSyntax.SyntaxTree && a.ApplicationSyntaxReference?.Span == attributeSyntax.Span);

            if (activeAttribute.AttributeClass?.Equals(attributeNamedType, SymbolEqualityComparer.Default) != true)
            {
                return;
            }

            ReportInvalidName(activeAttribute, context);
        }

        private static void ReportInvalidName(AttributeData attribute, SyntaxNodeAnalysisContext context)
        {
            var arguments = attribute.NamedArguments.Where(n => n.Key == nameof(QueryAttribute.OperationName)).ToArray();

            if (arguments.Length == 0)
            {
                return;
            }

            var operationName = arguments[0].Value.Value as string;

            if (GraphQLHelpers.IsValidName(operationName.AsSpan(), out var problems) == false)
            {
                context.ReportDiagnostic(
                    InvalidOperationNameDiagnostic.Create(operationName!, [.. problems], context.Node.GetLocation())
                );
            }
        }
    }
}