using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using QueryByShape.Analyzer.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace QueryByShape.Analyzer.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class VariableAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [InvalidVariableNameDiagnostic.Descriptor, DuplicateVariableDiagnostic.Descriptor]; 

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

            if (name is not "Variable" or nameof(VariableAttribute))
            {
                return;
            }

            var attributeNamedType = context.Compilation.ResolveNamedType<VariableAttribute>();
            var attributes = context.ContainingSymbol!.GetAttributes();
            var activeAttribute = attributes.Single(a => a.ApplicationSyntaxReference?.SyntaxTree == attributeSyntax.SyntaxTree && a.ApplicationSyntaxReference?.Span == attributeSyntax.Span);

            if (activeAttribute.IsAttributeType(attributeNamedType) is false
                 || activeAttribute.TryGetConstructorArgument(out string? activeName) is false)
            {
                return;
            }

            ReportInvalidName(activeName, context);
            ReportDuplicateNames(activeName, activeAttribute, attributeNamedType, attributes, context);
        }

        private static void ReportInvalidName(string name, SyntaxNodeAnalysisContext context)
        {
            var problems = new List<string>();

            if (name[0] != '$')
            {
                problems.Add("Must start with $");
            }
            else
            {
                GraphQLHelpers.IsValidName(name.AsSpan()[1..], out problems);
            }

            if (problems.Count > 0)
            {
                context.ReportDiagnostic(
                    InvalidVariableNameDiagnostic.Create(name, [.. problems], context.Node.GetLocation())
                );
            }
        }

        private static void ReportDuplicateNames(string name, AttributeData activeAttribute, INamedTypeSymbol attributeNamedType, ImmutableArray<AttributeData> attributes, SyntaxNodeAnalysisContext context)
        {
            var dupes = attributes.Where(a => a.IsAttributeType(attributeNamedType) && a.TryGetConstructorArgument(out string? argument) && argument == name);

            if (dupes.First() != activeAttribute)
            { 
                context.ReportDiagnostic(
                    DuplicateVariableDiagnostic.Create(name, activeAttribute.GetLocation())
                );
            }
        }
    }
}