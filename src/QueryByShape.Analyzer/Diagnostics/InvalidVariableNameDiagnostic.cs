using Microsoft.CodeAnalysis;

namespace QueryByShape.Analyzer.Diagnostics
{
    internal record InvalidVariableNameDiagnostic
    {
        internal static DiagnosticDescriptor Descriptor { get; } = DescriptorHelper.Create(
            id: 141,
            title: "Invalid variable name",
            messageFormat: "Variable name '{0}' is not valid. {1}",
            defaultSeverity: DiagnosticSeverity.Error
        );

        public static Diagnostic Create(string variableName, string[] reasons, Location location)
        {
            return Diagnostic.Create(Descriptor, location, [variableName, string.Join(". ", reasons)]);
        }
    }
}