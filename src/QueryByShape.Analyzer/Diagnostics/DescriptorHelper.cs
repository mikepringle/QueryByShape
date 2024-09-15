using Microsoft.CodeAnalysis;

namespace QueryByShape.Analyzer.Diagnostics
{
    internal static class DescriptorHelper
    {
        public const string Usage = nameof(Usage);

        public static DiagnosticDescriptor Create(int id, string title, string messageFormat, DiagnosticSeverity defaultSeverity = DiagnosticSeverity.Error) =>
            new (
                id: $"QBSHAPE{id.ToString().PadLeft(3, '0')}",
                title,
                messageFormat,
                category: Usage,
                defaultSeverity,
                isEnabledByDefault: true
            );
    }
}