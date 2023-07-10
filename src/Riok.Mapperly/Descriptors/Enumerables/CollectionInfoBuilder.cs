using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Riok.Mapperly.Helpers;

namespace Riok.Mapperly.Descriptors.Enumerables;

public static class CollectionInfoBuilder
{
    private readonly record struct CollectionTypeInfo(
        CollectionType CollectionType,
        Type? ReflectionType = null,
        string? TypeFullName = null,
        bool Immutable = false
    )
    {
        public INamedTypeSymbol? GetTypeSymbol(WellKnownTypes types)
        {
            if (ReflectionType != null)
                return types.Get(ReflectionType);

            if (TypeFullName != null)
                return types.TryGet(TypeFullName);

            throw new InvalidOperationException("One type needs to be set for each collection type");
        }
    }

    private static readonly CollectionTypeInfo _collectionTypeInfoArray = new(CollectionType.Array);

    private static readonly IReadOnlyCollection<CollectionTypeInfo> _collectionTypeInfos = new[]
    {
        new CollectionTypeInfo(CollectionType.IEnumerable, typeof(IEnumerable<>)),
        new CollectionTypeInfo(CollectionType.List, typeof(List<>)),
        new CollectionTypeInfo(CollectionType.Stack, typeof(Stack<>)),
        new CollectionTypeInfo(CollectionType.Queue, typeof(Queue<>)),
        new CollectionTypeInfo(CollectionType.IReadOnlyCollection, typeof(IReadOnlyCollection<>)),
        new CollectionTypeInfo(CollectionType.IList, typeof(IList<>)),
        new CollectionTypeInfo(CollectionType.IReadOnlyList, typeof(IReadOnlyList<>)),
        new CollectionTypeInfo(CollectionType.ICollection, typeof(ICollection<>)),
        new CollectionTypeInfo(CollectionType.HashSet, typeof(HashSet<>)),
        new CollectionTypeInfo(CollectionType.SortedSet, typeof(SortedSet<>)),
        new CollectionTypeInfo(CollectionType.ISet, typeof(ISet<>)),
        new CollectionTypeInfo(CollectionType.IReadOnlySet, TypeFullName: "System.Collections.Generic.IReadOnlySet`1"),
        new CollectionTypeInfo(CollectionType.IDictionary, typeof(IDictionary<,>)),
        new CollectionTypeInfo(CollectionType.IReadOnlyDictionary, typeof(IReadOnlyDictionary<,>)),
        new CollectionTypeInfo(CollectionType.Dictionary, typeof(Dictionary<,>)),
        new CollectionTypeInfo(CollectionType.ImmutableArray, typeof(ImmutableArray<>), Immutable: true),
        new CollectionTypeInfo(CollectionType.ImmutableList, typeof(ImmutableList<>), Immutable: true),
        new CollectionTypeInfo(CollectionType.IImmutableList, typeof(IImmutableList<>), Immutable: true),
        new CollectionTypeInfo(CollectionType.ImmutableHashSet, typeof(ImmutableHashSet<>), Immutable: true),
        new CollectionTypeInfo(CollectionType.IImmutableSet, typeof(IImmutableSet<>), Immutable: true),
        new CollectionTypeInfo(CollectionType.ImmutableSortedSet, typeof(ImmutableSortedSet<>), Immutable: true),
        new CollectionTypeInfo(CollectionType.ImmutableQueue, typeof(ImmutableQueue<>), Immutable: true),
        new CollectionTypeInfo(CollectionType.IImmutableQueue, typeof(IImmutableQueue<>), Immutable: true),
        new CollectionTypeInfo(CollectionType.ImmutableStack, typeof(ImmutableStack<>), Immutable: true),
        new CollectionTypeInfo(CollectionType.IImmutableStack, typeof(IImmutableStack<>), Immutable: true),
        new CollectionTypeInfo(CollectionType.IImmutableDictionary, typeof(IImmutableDictionary<,>), Immutable: true),
        new CollectionTypeInfo(CollectionType.ImmutableDictionary, typeof(ImmutableDictionary<,>), Immutable: true),
        new CollectionTypeInfo(CollectionType.ImmutableSortedDictionary, typeof(ImmutableSortedDictionary<,>), Immutable: true),
        new CollectionTypeInfo(CollectionType.Span, typeof(Span<>)),
        new CollectionTypeInfo(CollectionType.ReadOnlySpan, typeof(ReadOnlySpan<>)),
        new CollectionTypeInfo(CollectionType.Memory, typeof(Memory<>)),
        new CollectionTypeInfo(CollectionType.ReadOnlyMemory, typeof(ReadOnlyMemory<>)),
    };

    public static CollectionInfos? Build(WellKnownTypes wellKnownTypes, ITypeSymbol source, ITypeSymbol target)
    {
        // check for enumerated type to quickly check that both are collection types
        var enumeratedSourceType = GetEnumeratedType(wellKnownTypes, source);
        if (enumeratedSourceType == null)
            return null;

        var enumeratedTargetType = GetEnumeratedType(wellKnownTypes, target);
        if (enumeratedTargetType == null)
            return null;

        var sourceInfo = BuildCollectionInfo(wellKnownTypes, source, enumeratedSourceType);
        var targetInfo = BuildCollectionInfo(wellKnownTypes, target, enumeratedTargetType);

        return new CollectionInfos(sourceInfo, targetInfo);
    }

    private static CollectionInfo BuildCollectionInfo(WellKnownTypes wellKnownTypes, ITypeSymbol type, ITypeSymbol enumeratedType)
    {
        var collectionTypeInfo = GetCollectionTypeInfo(wellKnownTypes, type);
        var typeInfo = collectionTypeInfo?.CollectionType ?? CollectionType.None;

        return new CollectionInfo(
            type,
            typeInfo,
            GetImplementedCollectionTypes(wellKnownTypes, type, typeInfo),
            enumeratedType,
            FindCountProperty(wellKnownTypes, type, typeInfo),
            HasValidAddMethod(wellKnownTypes, type, typeInfo),
            collectionTypeInfo?.Immutable == true
        );
    }

    private static ITypeSymbol? GetEnumeratedType(WellKnownTypes types, ITypeSymbol type)
    {
        if (type.ImplementsGeneric(types.Get(typeof(IEnumerable<>)), out var enumerableIntf))
            return enumerableIntf.TypeArguments[0];

        // if type is not readonly struct with one type argument then return null
        if (type is not ({ IsValueType: true, IsReadOnly: true } and INamedTypeSymbol { TypeArguments.Length: 1 } namedType))
            return null;

        // if the collection is Span<> or Memory<> etc, get the type symbol
        if (
            SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, types.Get(typeof(Span<>)))
            || SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, types.Get(typeof(ReadOnlySpan<>)))
            || SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, types.Get(typeof(Memory<>)))
            || SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, types.Get(typeof(ReadOnlyMemory<>)))
        )
        {
            return namedType.TypeArguments[0];
        }

        return null;
    }

    private static bool HasValidAddMethod(WellKnownTypes types, ITypeSymbol t, CollectionType typeInfo)
    {
        if (
            typeInfo
            is CollectionType.ICollection
                or CollectionType.IList
                or CollectionType.List
                or CollectionType.ISet
                or CollectionType.HashSet
                or CollectionType.SortedSet
        )
            return true;

        if (typeInfo is not CollectionType.None)
            return false;

        return t.HasImplicitGenericImplementation(types.Get(typeof(ICollection<>)), nameof(ICollection<object>.Add))
            || t.HasImplicitGenericImplementation(types.Get(typeof(ISet<>)), nameof(ISet<object>.Add));
    }

    private static string? FindCountProperty(WellKnownTypes types, ITypeSymbol t, CollectionType typeInfo)
    {
        if (typeInfo is CollectionType.IEnumerable)
            return null;

        if (
            typeInfo
            is CollectionType.Array
                or CollectionType.Span
                or CollectionType.ReadOnlySpan
                or CollectionType.Memory
                or CollectionType.ReadOnlyMemory
        )
            return "Length";

        if (typeInfo is not CollectionType.None)
            return "Count";

        var intType = types.Get<int>();
        var member = t.GetAccessibleMappableMembers()
            .FirstOrDefault(
                x =>
                    x.Name is nameof(ICollection<object>.Count) or nameof(Array.Length)
                    && SymbolEqualityComparer.IncludeNullability.Equals(intType, x.Type)
            );
        return member?.Name;
    }

    private static CollectionTypeInfo? GetCollectionTypeInfo(WellKnownTypes types, ITypeSymbol type)
    {
        if (type.IsArrayType())
            return _collectionTypeInfoArray;

        // string is a collection but does implement IEnumerable, return early
        if (type.SpecialType == SpecialType.System_String)
            return null;

        foreach (var typeInfo in _collectionTypeInfos)
        {
            if (typeInfo.GetTypeSymbol(types) is not { } typeSymbol)
                continue;

            if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, typeSymbol))
                return typeInfo;
        }

        return null;
    }

    private static CollectionType GetImplementedCollectionTypes(WellKnownTypes types, ITypeSymbol type, CollectionType collectionType)
    {
        // if the collectionType is not CollectionType.None, return the known implemented types
        // this is done for performance reasons
        // when collectionType is None then manually check for implemented types with IterateImplementedTypes
        return collectionType switch
        {
            CollectionType.Array
                => CollectionType.Array
                    | CollectionType.IList
                    | CollectionType.IReadOnlyList
                    | CollectionType.IList
                    | CollectionType.ICollection
                    | CollectionType.IReadOnlyCollection
                    | CollectionType.IEnumerable,
            CollectionType.IEnumerable => CollectionType.IEnumerable,
            CollectionType.List
                => CollectionType.List
                    | CollectionType.IList
                    | CollectionType.IReadOnlyList
                    | CollectionType.ICollection
                    | CollectionType.IReadOnlyCollection
                    | CollectionType.IEnumerable,
            CollectionType.Stack => CollectionType.Stack | CollectionType.IReadOnlyCollection | CollectionType.IEnumerable,
            CollectionType.Queue => CollectionType.Queue | CollectionType.IReadOnlyCollection | CollectionType.IEnumerable,
            CollectionType.IReadOnlyCollection => CollectionType.IReadOnlyCollection | CollectionType.IEnumerable,
            CollectionType.IList => CollectionType.IList | CollectionType.ICollection | CollectionType.IEnumerable,
            CollectionType.IReadOnlyList => CollectionType.IReadOnlyList | CollectionType.IReadOnlyCollection | CollectionType.IEnumerable,
            CollectionType.ICollection => CollectionType.ICollection | CollectionType.IEnumerable,
            CollectionType.HashSet
                => CollectionType.HashSet
                    | CollectionType.ISet
                    | CollectionType.IReadOnlySet
                    | CollectionType.ICollection
                    | CollectionType.IReadOnlyCollection
                    | CollectionType.IEnumerable,
            CollectionType.SortedSet
                => CollectionType.SortedSet
                    | CollectionType.ISet
                    | CollectionType.IReadOnlySet
                    | CollectionType.ICollection
                    | CollectionType.IReadOnlyCollection
                    | CollectionType.IEnumerable,
            CollectionType.ISet => CollectionType.ISet | CollectionType.ICollection | CollectionType.IEnumerable,
            CollectionType.IReadOnlySet => CollectionType.IReadOnlySet | CollectionType.IReadOnlyCollection | CollectionType.IEnumerable,
            CollectionType.IDictionary => CollectionType.IDictionary | CollectionType.ICollection | CollectionType.IEnumerable,
            CollectionType.IReadOnlyDictionary
                => CollectionType.IReadOnlyDictionary | CollectionType.IReadOnlyCollection | CollectionType.IEnumerable,
            CollectionType.Dictionary
                => CollectionType.Dictionary
                    | CollectionType.IDictionary
                    | CollectionType.IReadOnlyDictionary
                    | CollectionType.ICollection
                    | CollectionType.IReadOnlyCollection
                    | CollectionType.IEnumerable,

            CollectionType.ImmutableArray
                => CollectionType.ImmutableArray
                    | CollectionType.IImmutableList
                    | CollectionType.IList
                    | CollectionType.IReadOnlyList
                    | CollectionType.ICollection
                    | CollectionType.IReadOnlyCollection
                    | CollectionType.IEnumerable,
            CollectionType.ImmutableList
                => CollectionType.ImmutableList
                    | CollectionType.IImmutableList
                    | CollectionType.IList
                    | CollectionType.IReadOnlyList
                    | CollectionType.ICollection
                    | CollectionType.IReadOnlyCollection
                    | CollectionType.IEnumerable,
            CollectionType.IImmutableList
                => CollectionType.IImmutableList
                    | CollectionType.IReadOnlyList
                    | CollectionType.IReadOnlyCollection
                    | CollectionType.IEnumerable,
            CollectionType.ImmutableHashSet
                => CollectionType.ImmutableHashSet
                    | CollectionType.IImmutableSet
                    | CollectionType.IReadOnlySet
                    | CollectionType.ISet
                    | CollectionType.ICollection
                    | CollectionType.IReadOnlyCollection
                    | CollectionType.IEnumerable,
            CollectionType.IImmutableSet => CollectionType.IImmutableSet | CollectionType.IReadOnlyCollection | CollectionType.IEnumerable,
            CollectionType.ImmutableSortedSet
                => CollectionType.ImmutableSortedSet
                    | CollectionType.IImmutableSet
                    | CollectionType.IList
                    | CollectionType.IReadOnlyList
                    | CollectionType.ISet
                    | CollectionType.IReadOnlySet
                    | CollectionType.ICollection
                    | CollectionType.IReadOnlyCollection
                    | CollectionType.IEnumerable,
            CollectionType.ImmutableQueue => CollectionType.ImmutableQueue | CollectionType.IImmutableQueue | CollectionType.IEnumerable,
            CollectionType.IImmutableQueue => CollectionType.IImmutableQueue | CollectionType.IEnumerable,
            CollectionType.ImmutableStack => CollectionType.ImmutableStack | CollectionType.IImmutableStack | CollectionType.IEnumerable,
            CollectionType.IImmutableStack => CollectionType.IImmutableStack | CollectionType.IEnumerable,
            CollectionType.ImmutableDictionary
                => CollectionType.ImmutableDictionary
                    | CollectionType.IImmutableDictionary
                    | CollectionType.IDictionary
                    | CollectionType.IReadOnlyDictionary
                    | CollectionType.ICollection
                    | CollectionType.IReadOnlyCollection
                    | CollectionType.IEnumerable,
            CollectionType.IImmutableDictionary
                => CollectionType.IImmutableDictionary
                    | CollectionType.IReadOnlyDictionary
                    | CollectionType.IReadOnlyCollection
                    | CollectionType.IEnumerable,
            CollectionType.ImmutableSortedDictionary
                => CollectionType.ImmutableSortedDictionary
                    | CollectionType.IImmutableDictionary
                    | CollectionType.IReadOnlyDictionary
                    | CollectionType.IReadOnlyCollection
                    | CollectionType.IEnumerable,
            CollectionType.Span => CollectionType.Span,
            CollectionType.ReadOnlySpan => CollectionType.ReadOnlySpan,
            CollectionType.Memory => CollectionType.Memory,
            CollectionType.ReadOnlyMemory => CollectionType.ReadOnlyMemory,

            // check for if the type is a string, returning IEnumerable
            CollectionType.None when type.SpecialType == SpecialType.System_String
                => CollectionType.IEnumerable,

            // fallback for CollectionType.None, manually checking for known implemented types
            _ => IterateImplementedTypes(type, types)
        };

        static CollectionType IterateImplementedTypes(ITypeSymbol type, WellKnownTypes types)
        {
            var implementedCollectionTypes = type.IsArrayType() ? CollectionType.Array : CollectionType.None;

            foreach (var typeInfo in _collectionTypeInfos)
            {
                if (typeInfo.GetTypeSymbol(types) is not { } typeSymbol)
                    continue;

                if (type.ImplementsGeneric(typeSymbol, out _))
                {
                    implementedCollectionTypes |= typeInfo.CollectionType;
                }
            }

            return implementedCollectionTypes;
        }
    }
}