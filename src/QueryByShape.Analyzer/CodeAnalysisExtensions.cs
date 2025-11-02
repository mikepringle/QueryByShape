using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace QueryByShape.Analyzer
{
    internal static class Extensions
    {
        public static string ToFullName(this AttributeData attribute)
        {
            return attribute.AttributeClass?
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", string.Empty) 
                ?? throw new ArgumentNullException(nameof(attribute));
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
            var name = typeof(T).FullName ?? throw new InvalidOperationException("No metadata name for type");
            var symbol = compilation.GetTypeByMetadataName(name);
            return symbol ?? throw new InvalidOperationException($"Type '{name}' not found in compilation");
        }

        public static bool TryGetConstructorArgument<T>(this AttributeData attribute, [NotNullWhen(true)] out T? value) 
        {            
            if (attribute?.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is T argument)
            {
                value = argument;
                return true;
            }

            value = default;
            return false;
        }

        public static bool TryGetConstructorArguments<TFirst, TSecond>(this AttributeData attribute, [NotNullWhen(true)] out TFirst? first, [NotNullWhen(true)] out TSecond? second)
        {
            if (attribute?.ConstructorArguments.Length > 1 && attribute.ConstructorArguments[0].Value is TFirst firstArg && attribute.ConstructorArguments[1].Value is TSecond secondArg)
            {
                first = firstArg;
                second = secondArg;
                return true;
            }

            first = default;
            second = default;
            return false;
        }

        public static bool TryGetNamedArgument<T>(this AttributeData attribute, string name, out T? value)
        {
            foreach (var kv in attribute.NamedArguments) 
            { 
                if (kv.Key == name && kv.Value.Value is T t) 
                { 
                    value = t; 
                    return true; 
                } 
            }
            
            value = default; 
            return false;
          }

        public static bool IsAttributeType(this AttributeData? attribute, INamedTypeSymbol? targetType)
        {
            var sourceType = attribute?.AttributeClass;

            if (sourceType is null || targetType is null)
            {
                return false;
            }

            return SymbolEqualityComparer.Default.Equals(sourceType, targetType);
        }

        public static string GetNamespace(this INamedTypeSymbol symbol)
        {
            return symbol.ContainingNamespace
                .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", string.Empty);
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

        public static Location ToTrimmedLocation(this Location location)
        {
            if (location == Location.None || location.SourceTree is null)
            {
                return location;
            }
            
            return Location.Create(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
        } 

    }
}
