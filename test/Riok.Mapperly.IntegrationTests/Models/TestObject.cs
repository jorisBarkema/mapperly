using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Riok.Mapperly.IntegrationTests.Models
{
    public class TestObject
    {
        public TestObject(int ctorValue, int unknownValue = 10, int ctorValue2 = 100)
        {
            CtorValue = ctorValue;
            CtorValue2 = ctorValue2;
        }

        public int CtorValue { get; set; }

        public int CtorValue2 { get; set; }

        public int IntValue { get; set; }

        public int IntInitOnlyValue { get; init; }

#if NET7_0_OR_GREATER
        public required int RequiredValue { get; init; }
#else
        public int RequiredValue { get; init; }
#endif

        public string StringValue { get; set; } = string.Empty;

        public string RenamedStringValue { get; set; } = string.Empty;

        public IdObject Flattening { get; set; } = new();

        public IdObject? NullableFlattening { get; set; }

        public int UnflatteningIdValue { get; set; }

        public int? NullableUnflatteningIdValue { get; set; }

        public TestObjectNested? NestedNullable { get; set; }

        public TestObjectNested? NestedNullableTargetNotNullable { get; set; }

        public string? StringNullableTargetNotNullable { get; set; }

        public (string A, string)? TupleValue { get; set; }

        public TestObject? RecursiveObject { get; set; }

        public TestObject? SourceTargetSameObjectType { get; set; }

        public IReadOnlyCollection<TestObjectNested>? NullableReadOnlyObjectCollection { get; set; }

        public Span<string> SpanValue => new[] { "1", "2", "3" };

        public Memory<string> MemoryValue { get; set; }

        public Stack<string> StackValue { get; set; } = new();

        public Queue<string> QueueValue { get; set; } = new();

        public ImmutableArray<string> ImmutableArrayValue { get; set; } = ImmutableArray<string>.Empty;

        public ImmutableList<string> ImmutableListValue { get; set; } = ImmutableList<string>.Empty;

        public ImmutableHashSet<string> ImmutableHashSetValue { get; set; } = ImmutableHashSet<string>.Empty;

        public ImmutableQueue<string> ImmutableQueueValue { get; set; } = ImmutableQueue<string>.Empty;

        public ImmutableStack<string> ImmutableStackValue { get; set; } = ImmutableStack<string>.Empty;

        public ImmutableSortedSet<string> ImmutableSortedSetValue { get; set; } = ImmutableSortedSet<string>.Empty;

        public ImmutableDictionary<string, string> ImmutableDictionaryValue { get; set; } = ImmutableDictionary<string, string>.Empty;

        public ImmutableSortedDictionary<string, string> ImmutableSortedDictionaryValue { get; set; } =
            ImmutableSortedDictionary<string, string>.Empty;

        public ISet<string> ExistingISet { get; } = new HashSet<string>();

        public HashSet<string> ExistingHashSet { get; } = new HashSet<string>();

        public SortedSet<string> ExistingSortedSet { get; } = new SortedSet<string>();

        public ISet<string> ISet { get; set; } = new HashSet<string>();

#if NET5_0_OR_GREATER
        public IReadOnlySet<string> IReadOnlySet { get; set; } = new HashSet<string>();
#endif

        public HashSet<string> HashSet { get; set; } = new HashSet<string>();

        public SortedSet<string> SortedSet { get; set; } = new SortedSet<string>();

        public TestEnum EnumValue { get; set; }

        public TestFlagsEnum FlagsEnumValue { get; set; }

        public TestEnum EnumName { get; set; }

        public TestEnum EnumRawValue { get; set; }

        public TestEnum EnumStringValue { get; set; }

        public string EnumReverseStringValue { get; set; } = string.Empty;

        public InheritanceSubObject? SubObject { get; set; }

        public string? IgnoredStringValue { get; set; }

        public int IgnoredIntValue { get; set; }

        public DateTime DateTimeValueTargetDateOnly { get; set; }

        public DateTime DateTimeValueTargetTimeOnly { get; set; }
    }
}
