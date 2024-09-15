using Microsoft.CodeAnalysis;

namespace QueryByShape.Analyzer.Diagnostics
{
    internal record QueryMustBePartialDiagnostic
    {
        internal static DiagnosticDescriptor Descriptor { get; } = DescriptorHelper.Create(
            id: 10,
            title: "Must be Partial",
            messageFormat: "The class '{0}' decorated with the QueryAttribute must be partial"
        );

        public static DiagnosticMetadata Create(string className, Location location)
        {
            return new DiagnosticMetadata(Descriptor, location.ToTrimmedLocation(), new EquatableArray<string>([className]));
        }
    }
}
