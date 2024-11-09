using Microsoft.CodeAnalysis;

namespace QueryByShape.Analyzer.Diagnostics
{
    internal record MissingVariableDiagnostic
    {
        internal static DiagnosticDescriptor Descriptor { get; } = DescriptorHelper.Create(
            id: 20,
            title: "Missing Variable",
            messageFormat: "The variable '{1}' was not found.  (refenced in argument '{0}')"
        );

        public static DiagnosticMetadata CreateMetadata(string argumentName, string variableName, Location location)
        {
            return new DiagnosticMetadata(Descriptor, location.ToTrimmedLocation(), [argumentName, variableName]);
        }
    }
}
