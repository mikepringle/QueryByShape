using Microsoft.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace QueryByShape.Analyzer.Diagnostics
{
    internal record DuplicateArgumentDiagnostic
    {
        internal static DiagnosticDescriptor Descriptor { get; } = DescriptorHelper.Create(
            id: 30,
            title: "Duplicate Argument Names",
            messageFormat: "Argument names must be unique. '{0}' already exists."
        );

        public static DiagnosticMetadata Create(string argumentName, Location location)
        {
            return new DiagnosticMetadata(Descriptor, location.ToTrimmedLocation(), new EquatableArray<string>([argumentName]));
        }
    }
}