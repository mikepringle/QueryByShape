using Microsoft.CodeAnalysis;

namespace QueryByShape.Analyzer.Diagnostics
{
    internal record UnusedVariableDiagnostic
    {
        internal static DiagnosticDescriptor Descriptor { get; } = DescriptorHelper.Create(
            id: 23,
            title: "Unused Variable",
            messageFormat: "Variable '{0}' is not referenced",
            defaultSeverity: DiagnosticSeverity.Warning
        );

        public static DiagnosticMetadata CreateMetadata(string variableName, Location location)
        {
            return new DiagnosticMetadata(Descriptor, location.ToTrimmedLocation(), [variableName]);
        }
    }
}
