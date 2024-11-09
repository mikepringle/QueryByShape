using Microsoft.CodeAnalysis;

namespace QueryByShape.Analyzer.Diagnostics
{
    internal record InvalidOperationNameDiagnostic
    {
        internal static DiagnosticDescriptor Descriptor { get; } = DescriptorHelper.Create(
            id: 100,
            title: "Invalid operation name",
            messageFormat: "Operation name '{0}' is not valid. {1}",
            defaultSeverity: DiagnosticSeverity.Error
        );

        public static Diagnostic Create(string variableName, string[] reasons, Location location)
        {
            return Diagnostic.Create(Descriptor, location, [variableName, string.Join(". ", reasons)]);
        }
    }
}