using Microsoft.CodeAnalysis;

namespace QueryByShape.Analyzer.Diagnostics
{
    internal record QueryMustImplementDiagnostic
    {
        internal static DiagnosticDescriptor Descriptor { get; } = DescriptorHelper.Create(
            id: 101,
            title: "Must implement IGeneratedQuery",
            messageFormat: "The class '{0}' decorated with the QueryAttribute must implement IGeneratedQuery"
        );

        public static Diagnostic Create(string className, Location location)
        {
            return Diagnostic.Create(Descriptor, location, [className]);
        }
    }
}
