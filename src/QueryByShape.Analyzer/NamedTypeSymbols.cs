using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Reflection;

namespace QueryByShape.Analyzer
{
    public class NamedTypeSymbols(Compilation compilation)
    {
        public INamedTypeSymbol Delegate => _delegate ??= compilation.GetSpecialType(SpecialType.System_Delegate);
        private INamedTypeSymbol? _delegate;

        public INamedTypeSymbol IEnumerableOfT => _iEnumerableOfT ??= compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);
        private INamedTypeSymbol? _iEnumerableOfT;

        public INamedTypeSymbol IDictionaryOfKV => _iDictionaryOfKV ??= compilation.GetTypeByMetadataName(typeof(IDictionary<,>).FullName)!;
        private INamedTypeSymbol? _iDictionaryOfKV;

        public INamedTypeSymbol IntPtr => _intPtr ??= compilation.GetSpecialType(SpecialType.System_IntPtr);
        private INamedTypeSymbol? _intPtr;

        public INamedTypeSymbol MemberInfo => _memberInfo ??= compilation.GetTypeByMetadataName(typeof(MemberInfo).FullName)!;
        private INamedTypeSymbol? _memberInfo;

        public INamedTypeSymbol UIntPtr => _uIntPtr ??= compilation.GetSpecialType(SpecialType.System_UIntPtr);
        private INamedTypeSymbol? _uIntPtr;

    
        public bool IsPropertySerializable(IPropertySymbol property)
        {
            return (
                property.DeclaredAccessibility is Accessibility.Public
                && property.IsStatic is false
                && property.Parameters.Length == 0
                && property.IsReadOnly is false
                && property.IsWriteOnly is false
            );
        }

        public bool IsFieldSerializable(IFieldSymbol field)
        {
            return (
                field.DeclaredAccessibility is Accessibility.Public
                && field.IsStatic is false
                && field.IsConst is false
                && field.AssociatedSymbol is null
                && field.IsExplicitlyNamedTupleElement is false
                && field.IsReadOnly is false
            );
        }

        public bool IsTypeSerializable(INamedTypeSymbol type)
        {
            return (
                Delegate.IsAssignableFrom(type)
                    || MemberInfo.IsAssignableFrom(type)
                    || IDictionaryOfKV.IsAssignableFrom(type)
                    || type.Equals(IntPtr, SymbolEqualityComparer.Default)
                    || type.Equals(UIntPtr, SymbolEqualityComparer.Default)
            ) is false;
        }

        public bool TryGetChildrenType(ITypeSymbol type, out INamedTypeSymbol? childrenType)
        {
            childrenType = null;
            ITypeSymbol? effectiveType = null;

            if (CanHaveChildren(type) == false)
            {
                return false;
            }

            if (type is IArrayTypeSymbol arrayType)
            {
                effectiveType = arrayType.ElementType;
            }
            else if (type is not INamedTypeSymbol namedTypeSymbol)
            {
                return false;
            }
            else if (namedTypeSymbol.TryGetCompatibleGenericBaseType(IEnumerableOfT, out var instance))
            {
                effectiveType = instance?.TypeArguments[0] as INamedTypeSymbol;
            }
                        
            bool hasChildren = effectiveType == null || CanHaveChildren(effectiveType);
            childrenType = hasChildren ? (effectiveType ?? type) as INamedTypeSymbol : null;
            return hasChildren;
        }

        private bool CanHaveChildren(ITypeSymbol type)
        {
            if ((type.IsValueType && type.SpecialType != SpecialType.None) || type.SpecialType == SpecialType.System_String)
            {
                return false;
            }

            return true;
        }
    }
}
