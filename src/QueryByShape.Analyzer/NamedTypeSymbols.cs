using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace QueryByShape.Analyzer
{
    public struct LazySymbol(Func<INamedTypeSymbol> creator)
    {
        private INamedTypeSymbol? _value = null;
        public INamedTypeSymbol Value => _value ?? (_value = creator());
    }
    public class NamedTypeSymbols
    {
        private readonly Compilation _compilation;

        public NamedTypeSymbols(Compilation compilation)
        {
            _compilation = compilation;
            _delegate = new (() => ResolveNamedType(typeof(Delegate).FullName));
            _iEnumerableOfT = new(() => ResolveNamedType(typeof(IEnumerable<>).FullName));
            _iDictionaryOfKV = new (() => ResolveNamedType(typeof(IDictionary<,>).FullName));
            _intPtr = new (() => ResolveNamedType(typeof(IntPtr).FullName));
            _memberInfo = new (() => ResolveNamedType(typeof(MemberInfo).FullName));
            _uIntPtr = new (() => ResolveNamedType(typeof(UIntPtr).FullName));
        }

        public INamedTypeSymbol Delegate => _delegate.Value;
        private readonly LazySymbol _delegate;

        public INamedTypeSymbol IEnumerableOfT => _iEnumerableOfT.Value;
        private readonly LazySymbol _iEnumerableOfT;

        public INamedTypeSymbol IDictionaryOfKV => _iDictionaryOfKV.Value;
        private readonly LazySymbol _iDictionaryOfKV;

        public INamedTypeSymbol IntPtr => _intPtr.Value;
        private readonly LazySymbol _intPtr;

        public INamedTypeSymbol MemberInfo => _memberInfo.Value;
        private readonly LazySymbol _memberInfo;

        public INamedTypeSymbol UIntPtr => _uIntPtr.Value;
        private readonly LazySymbol _uIntPtr;

        private INamedTypeSymbol ResolveNamedType(string name)
        {
            return _compilation.GetTypeByMetadataName(name)!;
        }




        public (bool isSerializable, INamedTypeSymbol? childType) GetMemberInfo(ISymbol member)
        {
            if (TryGetSerializableInfo(member, out var memberType) == false)
            {
                return (false, null);
            }
            
            TryGetChildrenType(memberType!, out var childrenType);
            return (true, childrenType);
        }

        public bool TryGetSerializableInfo(ISymbol member, out INamedTypeSymbol? symbol)
        {
            symbol = member switch
            {
                IPropertySymbol property when IsPropertySerializable(property) => property.Type,
                IFieldSymbol field when IsFieldSerializable(field) => field.Type,
                _ => null
            } as INamedTypeSymbol;

            return symbol != null;
        }

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

        private bool TryGetChildrenType(ITypeSymbol type, out INamedTypeSymbol? childrenType)
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
