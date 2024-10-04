using Microsoft.CodeAnalysis;
using System;
using System.Linq;
using System.Text.Json.Serialization;

namespace QueryByShape.Analyzer
{
    internal static class Extensions
    {
        public static string ToFullName(this AttributeData attribute)
        {
            var attributeClass = attribute.AttributeClass ?? throw new NullReferenceException();
            return $"{attributeClass.GetNamespace()}.{attributeClass.Name}";    
        }

        public static Location GetLocation(this SyntaxReference? reference)
        {
            return reference?.SyntaxTree.GetLocation(reference.Span) ?? Location.None;
        }

        public static Location GetLocation(this AttributeData attribute)
        {
            return attribute.ApplicationSyntaxReference?.GetLocation() ?? Location.None;
        }

        public static string GetConstructorArgument(this AttributeData attribute) => GetConstructorArguments(attribute)[0];
        public static string[] GetConstructorArguments(this AttributeData attribute) => attribute.ConstructorArguments.Select(c => c.Value?.ToString() ?? "").ToArray();
        
        public static bool TryGetNamedArgument<T>(this AttributeData attribute, string name, out T? value)
        {
            value = default;

            var arguments = attribute.NamedArguments.Where(a => a.Key == name);
            
            if (arguments.Any())
            {
                value = (T)arguments.First().Value.Value; 
                return true;
            }

            return false;
        }

        public static string GetNamespace(this INamedTypeSymbol symbol)
        {
            return string.Join(".", GetNamespace_Internal(symbol.ContainingNamespace));
            
            string[] GetNamespace_Internal(INamespaceSymbol symbol, int index = 0)
            {
                if (symbol.ContainingNamespace == null) 
                {
                    return new string[index];
                }
                
                var result = GetNamespace_Internal(symbol.ContainingNamespace, index + 1);
                result[result.Length - index - 1] = symbol.Name;
                return result;
            }
        }

        public static bool IsImplementing(this INamedTypeSymbol type, INamedTypeSymbol from)
        {
            return type.IsGenericType && type.ConstructedFrom.Equals(from, SymbolEqualityComparer.Default);
        }

        public static bool IsAssignableTo(this INamedTypeSymbol type, INamedTypeSymbol to)
        {
            if (to.TypeKind == TypeKind.Interface)
            {
                return type.IsImplementing(to) || type.BaseType?.AllInterfaces.Any(a => a.IsImplementing(to)) == true;
            }
            else
            {
                if (type.Equals(to, SymbolEqualityComparer.Default))
                {
                    return true;
                }

                var baseType = to.BaseType;


                while (baseType is not null)
                {
                    if (type.Equals(to, SymbolEqualityComparer.Default))
                    {
                        return true;
                    }

                    baseType = baseType.BaseType;
                }

                return false;
            }
        }
    
        public static Location ToTrimmedLocation(this Location location) => Location.Create(location.SourceTree!.FilePath, location.SourceSpan, location.GetLineSpan().Span);
    }
}
