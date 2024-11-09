using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace QueryByShape.Analyzer
{
    internal static class Extensions
    {
        public static string ToFullName(this AttributeData attribute)
        {
            var attributeClass = attribute.AttributeClass ?? throw new NullReferenceException();
            return $"{attributeClass.GetNamespace()}.{attributeClass.Name}";    
        }

        public static string ExtractName(this NameSyntax nameSyntax)
        {
            return nameSyntax switch
            {
                SimpleNameSyntax ins => ins.Identifier.Text,
                QualifiedNameSyntax qns => qns.Right.Identifier.Text,
                _ => throw new NotSupportedException()
            };
        }

        public static AttributeData? GetAttributeData(this AttributeSyntax syntax, ISymbol parentSymbol)
        {
            var parentAttributes = parentSymbol.GetAttributes();
            var syntaxRef = syntax.SyntaxTree;
            var syntaxSpan = syntax.Span;

            return parentAttributes.FirstOrDefault(a => a.ApplicationSyntaxReference?.SyntaxTree == syntaxRef && a.ApplicationSyntaxReference?.Span == syntaxSpan);
        }

        public static Location GetLocation(this SyntaxReference? reference)
        {
            return reference?.SyntaxTree.GetLocation(reference.Span) ?? Location.None;
        }

        public static Location GetLocation(this AttributeData attribute)
        {
            return attribute.ApplicationSyntaxReference?.GetLocation() ?? Location.None;
        }

        public static INamedTypeSymbol ResolveNamedType<T>(this Compilation compilation)
        {
            return compilation.GetTypeByMetadataName(typeof(T).FullName)!;
        }

        public static string GetConstructorArgument(this AttributeData attribute) => GetConstructorArguments(attribute)[0];
        public static string[] GetConstructorArguments(this AttributeData attribute) => attribute.ConstructorArguments.Select(c => c.Value?.ToString() ?? "").ToArray();
        
        public static bool TryGetNamedArgument<T>(this AttributeData attribute, string name, out T? value)
        {
            value = default;

            var arguments = attribute.NamedArguments.Where(a => a.Key == name);
            
            if (arguments.Any())
            {
                value = (T?)arguments.First().Value.Value; 
                return true;
            }

            return false;
        }

        public static string GetNamespace(this INamedTypeSymbol symbol)
        {
            return string.Join(".", GetNamespace_Internal(symbol.ContainingNamespace));
            
            static string[] GetNamespace_Internal(INamespaceSymbol symbol, int index = 0)
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

        public static bool TryGetCompatibleGenericBaseType(this ITypeSymbol type, INamedTypeSymbol? baseType, out INamedTypeSymbol? result)
        {
            result = null;

            if (baseType is null)
            {
                return false;
            }

            if (baseType.TypeKind is TypeKind.Interface)
            {
                foreach (INamedTypeSymbol interfaceType in type.AllInterfaces)
                {
                    if (IsMatchingGenericType(interfaceType, baseType))
                    {
                        result = interfaceType;
                        return true;
                    }
                }
            }

            for (INamedTypeSymbol? current = type as INamedTypeSymbol; current != null; current = current.BaseType)
            {
                if (IsMatchingGenericType(current, baseType))
                {
                    result = current;
                    return true;
                }
            }

            return false;

            static bool IsMatchingGenericType(INamedTypeSymbol candidate, INamedTypeSymbol baseType)
            {
                return candidate.IsGenericType && SymbolEqualityComparer.Default.Equals(candidate.ConstructedFrom, baseType);
            }
        }


        public static bool IsAssignableFrom(this ITypeSymbol? baseType, ITypeSymbol? type)
        {
            if (baseType is null || type is null)
            {
                return false;
            }

            if (baseType.TypeKind is TypeKind.Interface)
            {
                if (type.AllInterfaces.Contains(baseType, SymbolEqualityComparer.Default))
                {
                    return true;
                }
            }

            for (INamedTypeSymbol? current = type as INamedTypeSymbol; current != null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(baseType, current))
                {
                    return true;
                }
            }

            return false;
        }

        public static Location ToTrimmedLocation(this Location location) => Location.Create(location.SourceTree!.FilePath, location.SourceSpan, location.GetLineSpan().Span);
    }
}
