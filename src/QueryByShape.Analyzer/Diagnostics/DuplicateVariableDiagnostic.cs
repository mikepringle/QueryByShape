using Microsoft.CodeAnalysis;

namespace QueryByShape.Analyzer.Diagnostics
{
    internal record DuplicateVariableDiagnostic
    {
        internal static DiagnosticDescriptor Descriptor { get; } = DescriptorHelper.Create(
            id: 25,
            title: "Duplicate Variable Names",
            messageFormat: "Variable names must be unique. '{0}' already exists."
        );

        public static DiagnosticMetadata Create(string variableName, Location location)
        {
            return new DiagnosticMetadata(Descriptor, location.ToTrimmedLocation(), new EquatableArray<string>([variableName]));
        }
    }
}
