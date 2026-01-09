using Microsoft.CodeAnalysis;
using System.Reflection;

namespace QueryByShape.Analyzer
{

    internal record QueryOptions()
    {
        public bool IncludeFields { get; set; } = false;

        public JsonPropertyNaming PropertyNamingPolicy { get; set; } = JsonPropertyNaming.CamelCase;

        public SourceFormatting Formatting { get; set; } = SourceFormatting.Minified;
    }

    internal record DiagnosticMetadata(DiagnosticDescriptor Descriptor, Location Location, EquatableArray<string>? MessageArguments = null);

    internal record QueryMetadata(string TypeName, string NamespaceName)
    {
        public TypeMetadata? Type { get; set; }

        public string? Name { get; set; }

        public EquatableArray<VariableMetadata>? Variables { get; set; }

        public QueryOptions Options { get; set; } = new QueryOptions();
    }

    internal record TypeMetadata(EquatableArray<MemberMetadata> Members);
 
    internal record MemberMetadata(string Name, SymbolKind Kind)
    {
        public string? OverrideName { get; set; }

        public string? AliasOf { get; set; }

        public bool? Ignore { get; set; }

        public EquatableArray<ArgumentMetadata>? Arguments { get; set; }

        public EquatableArray<string>? On { get; set; }

        public TypeMetadata? ChildrenType { get; set; }
    }

    internal readonly record struct ArgumentMetadata(string Name, string VariableName);

    internal readonly record struct VariableMetadata(string Name, string GraphType, object? DefaultValue, SyntaxReference? Reference);
}
