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
    public class ArgumentAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [InvalidArgumentNameDiagnostic.Descriptor, DuplicateArgumentDiagnostic.Descriptor];

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

            if (name is not "Argument" or nameof(ArgumentAttribute))
            {
                return;
            }

            var attributeNamedType = context.Compilation.ResolveNamedType<ArgumentAttribute>();
            var attributes = context.ContainingSymbol!.GetAttributes();
            var activeAttribute = attributes.Single(a => a.ApplicationSyntaxReference?.SyntaxTree == attributeSyntax.SyntaxTree && a.ApplicationSyntaxReference?.Span == attributeSyntax.Span);
            
            if (activeAttribute.AttributeClass?.Equals(attributeNamedType, SymbolEqualityComparer.Default) != true)
            {
                return;
            }

            var activeName = activeAttribute.GetConstructorArgument();
            ReportInvalidName(activeName, context);
            ReportDuplicateNames(activeName, activeAttribute, attributeNamedType, attributes, context);

        }

        private static void ReportInvalidName(string name, SyntaxNodeAnalysisContext context)
        {
            if (GraphQLHelpers.IsValidName(name.AsSpan(), out var problems) == false)
            {
                context.ReportDiagnostic(
                    InvalidArgumentNameDiagnostic.Create(name, [.. problems], context.Node.GetLocation())
                );
            }
        }

        private static void ReportDuplicateNames(string name, AttributeData activeAttribute, INamedTypeSymbol attributeNamedType, ImmutableArray<AttributeData> attributes, SyntaxNodeAnalysisContext context)
        {
            var dupes = attributes.Where(a => a.AttributeClass?.Equals(attributeNamedType, SymbolEqualityComparer.Default) == true && a.GetConstructorArgument() == name);

            if (dupes.First() != activeAttribute)
            {
                context.ReportDiagnostic(
                    DuplicateArgumentDiagnostic.Create(name, activeAttribute.GetLocation())
                );
            }
        }
    }
}