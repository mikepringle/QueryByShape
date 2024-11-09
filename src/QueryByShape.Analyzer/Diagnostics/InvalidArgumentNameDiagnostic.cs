using Microsoft.CodeAnalysis;

namespace QueryByShape.Analyzer.Diagnostics
{
    internal record InvalidArgumentNameDiagnostic
    {
        internal static DiagnosticDescriptor Descriptor { get; } = DescriptorHelper.Create(
            id: 111,
            title: "Invalid argumemnt name",
            messageFormat: "Argument name '{0}' is not valid. {1}",
            defaultSeverity: DiagnosticSeverity.Error
        );

        public static Diagnostic Create(string variableName, string[] reasons, Location location)
        {
            return Diagnostic.Create(Descriptor, location, [variableName, string.Join(". ", reasons)]);
        }
    }
}