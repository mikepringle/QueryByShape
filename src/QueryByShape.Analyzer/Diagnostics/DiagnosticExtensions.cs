using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace QueryByShape.Analyzer.Diagnostics
{
    internal static class DiagnosticExtensions
    {
        public static Diagnostic ToDiagnostic(this DiagnosticMetadata metadata)
        {
            return Diagnostic.Create(metadata.Descriptor, metadata.Location, metadata.MessageArguments.ToMessageArgs());
        }

        public static object[]? ToMessageArgs(this EquatableArray<string>? arguments)
        {
            if (arguments?.Count is not > 0)
            {
                return null;
            }

            object[] messageArgs = new object[arguments.Value.Count];
            Array.Copy(arguments.Value.ToArray(), messageArgs, arguments.Value.Count);
            return messageArgs;
        }
    }
}
