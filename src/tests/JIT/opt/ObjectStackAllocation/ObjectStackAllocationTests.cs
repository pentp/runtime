// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

namespace ObjectStackAllocation
{
    class SimpleClassA
    {
        public int f1;
        public int f2;

        public SimpleClassA(int f1, int f2)
        {
            this.f1 = f1;
            this.f2 = f2;
        }
    }

    sealed class SimpleClassB
    {
        public long f1;
        public long f2;

        public SimpleClassB(long f1, long f2)
        {
            this.f1 = f1;
            this.f2 = f2;
        }
    }

    sealed class SimpleClassWithGCField : SimpleClassA
    {
        public object o;

        public SimpleClassWithGCField(int f1, int f2, object o) : base(f1, f2)
        {
            this.o = o;
        }
    }

    class ClassWithGCFieldAndInt
    {
        public object o;
        public int i;

        public ClassWithGCFieldAndInt(int i, object o)
        {
            this.o = o;
            this.i = i;
        }
    }

    class ClassWithNestedStruct
    {
        public ClassWithNestedStruct(int f1, int f2)
        {
            ns.f1 = f1;
            ns.f2 = f2;
            ns.s.f1 = f1;
            ns.s.f2 = f2;
        }

        public NestedStruct ns;
    }

    struct SimpleStruct
    {
        public int f1;
        public int f2;
    }

    struct NestedStruct
    {
        public int f1;
        public int f2;
        public SimpleStruct s;
    }

    struct GCStruct
    {
        public int i;
        public int[] o1;
        public int[] o2;
    }

    struct GCStruct2
    {
        public string[] a;
        public string[] b;
    }

    enum AllocationKind
    {
        Heap,
        Stack,
        Undefined
    }

    ref struct SpanKeeper<T>
    {
        public int a;
        public Span<T> span;
        public int b;
    }

    public class Tests
    {
        static volatile int f1 = 5;
        static volatile int f2 = 7;
        static SimpleClassA classA;
        static SimpleClassWithGCField classWithGCField;
        static string str0;
        static string str1;
        static string str2;
        static string str3;
        static string str4;

        delegate int Test();

        static int methodResult = 100;

        [Fact]
        public static int TestEntryPoint()
        {
            AllocationKind expectedAllocationKind = AllocationKind.Stack;
            if (GCStressEnabled())
            {
                Console.WriteLine("GCStress is enabled");
                expectedAllocationKind = AllocationKind.Undefined;
            }

            if (expectedAllocationKind == AllocationKind.Stack)
            {
                ZeroAllocTest();
            }

            classA = new SimpleClassA(f1, f2);

            classWithGCField = new SimpleClassWithGCField(f1, f2, null);

            str0 = "str_zero";
            str1 = "str_one";
            str2 = "str_two";
            str3 = "str_three";
            str4 = "str_four";

            CallTestAndVerifyAllocation(AllocateSimpleClassAndAddFields, 12, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateSimpleClassesAndEQCompareThem, 0, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateSimpleClassesAndNECompareThem, 1, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateSimpleClassAndGetField, 7, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateClassWithNestedStructAndGetField, 5, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateClassWithNestedStructAndAddFields, 24, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateSimpleClassWithGCFieldAndAddFields, 12, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateSimpleClassAndAssignRefToAField, 12, expectedAllocationKind);

            CallTestAndVerifyAllocation(TestMixOfReportingAndWriteBarriers, 34, expectedAllocationKind);

            // The object is currently allocated on the stack when this method is jitted and on the heap when it's R2R-compiled.
            // The reason is that we always do the type check via helper in R2R mode, which blocks stack allocation.
            // We don't have to use a helper in this case (even for R2R), https://github.com/dotnet/runtime/issues/11850 tracks fixing that.
            CallTestAndVerifyAllocation(AllocateSimpleClassAndCheckTypeNoHelper, 1, AllocationKind.Undefined);

            CallTestAndVerifyAllocation(AllocateClassWithGcFieldAndInt, 5, expectedAllocationKind);

            // Stack allocation of boxed structs is now enabled
            CallTestAndVerifyAllocation(BoxSimpleStructAndAddFields, 12, expectedAllocationKind);

            // Fixed-sized stack array cases
            CallTestAndVerifyAllocation(AllocateArrayWithNonGCElements, 84, expectedAllocationKind);
            CallTestAndVerifyAllocation(AllocateArrayWithGCElements, 84, expectedAllocationKind);
            CallTestAndVerifyAllocation(AllocateArrayT<int>, 84, expectedAllocationKind);
            CallTestAndVerifyAllocation(AllocateArrayT<string>, 84, expectedAllocationKind);

            // Spans
            CallTestAndVerifyAllocation(SpanCaptureArray1, 41, expectedAllocationKind);
            CallTestAndVerifyAllocation(SpanCaptureArray2, 25, expectedAllocationKind);
            CallTestAndVerifyAllocation(SpanCaptureArrayT<int>, 37, expectedAllocationKind);
            CallTestAndVerifyAllocation(SpanCaptureArrayT<string>, 37, expectedAllocationKind);

            // Other structs with GC fields.
            CallTestAndVerifyAllocation(StructReferredObjects, 25, expectedAllocationKind);

            // The remaining tests currently never allocate on the stack
            if (expectedAllocationKind == AllocationKind.Stack)
            {
                expectedAllocationKind = AllocationKind.Heap;
            }

            // This test calls CORINFO_HELP_ISINSTANCEOFCLASS
            CallTestAndVerifyAllocation(AllocateSimpleClassAndCheckTypeHelper, 1, expectedAllocationKind);

            // This test calls CORINFO_HELP_CHKCASTCLASS_SPECIAL
            CallTestAndVerifyAllocation(AllocateSimpleClassAndCast, 7, expectedAllocationKind);

            CallTestAndVerifyAllocation(AllocateArrayWithNonGCElementsEscape, 42, expectedAllocationKind);
            CallTestAndVerifyAllocation(AllocateArrayWithGCElementsEscape, 42, expectedAllocationKind);

            CallTestAndVerifyAllocation(SpanEscapeArrayArg, 42, expectedAllocationKind);
            CallTestAndVerifyAllocation(SpanEscapeArrayArgCopy, 42, expectedAllocationKind);
            CallTestAndVerifyAllocation(SpanEscapeArrayOutParam, 22, expectedAllocationKind);
            CallTestAndVerifyAllocation(SpanEscapeArrayOutParam2, 22, expectedAllocationKind);
            CallTestAndVerifyAllocation(SpanEscapeRef, 55, expectedAllocationKind);

            // Structs
            CallTestAndVerifyAllocation(StructReferredObjectEscape1, 33, expectedAllocationKind);
            CallTestAndVerifyAllocation(StructReferredObjectEscape2, 33, expectedAllocationKind);
            CallTestAndVerifyAllocation(StructReferredObjectEscape3, 41, expectedAllocationKind);
            CallTestAndVerifyAllocation(StructReferredObjectEscape4, 41, expectedAllocationKind);
            CallTestAndVerifyAllocation(StructReferredObjectEscape5, 5, expectedAllocationKind);

            // This test calls CORINFO_HELP_OVERFLOW
            CallTestAndVerifyAllocation(AllocateArrayWithNonGCElementsOutOfRangeLeft, 0, expectedAllocationKind, true);

            // This test calls CORINFO_HELP_OVERFLOW
            CallTestAndVerifyAllocation(AllocateArrayWithNonGCElementsOutOfRangeRight, 0, expectedAllocationKind, true);

            // This test calls CORINFO_HELP_ARTHEMIC_OVERFLOW
            CallTestAndVerifyAllocation(AllocateNegativeLengthArrayWithNonGCElements, 0, expectedAllocationKind, true);

            // This test calls CORINFO_HELP_ARTHEMIC_OVERFLOW
            CallTestAndVerifyAllocation(AllocateLongLengthArrayWithNonGCElements, 0, expectedAllocationKind, true);

            return methodResult;
        }

        static bool GCStressEnabled()
        {
            return Environment.GetEnvironmentVariable("DOTNET_GCStress") != null;
        }

        static void CallTestAndVerifyAllocation(Test test, int expectedResult, AllocationKind expectedAllocationsKind, bool throws = false)
        {
            string methodName = test.Method.Name;
            try
            {
                long allocatedBytesBefore = GC.GetAllocatedBytesForCurrentThread();
                int testResult = test();
                long allocatedBytesAfter = GC.GetAllocatedBytesForCurrentThread();

                if (testResult != expectedResult)
                {
                    Console.WriteLine($"FAILURE ({methodName}): expected {expectedResult}, got {testResult}");
                    methodResult = -1;
                }
                else if ((expectedAllocationsKind == AllocationKind.Stack) && (allocatedBytesBefore != allocatedBytesAfter))
                {
                    Console.WriteLine($"FAILURE ({methodName}): unexpected allocation of {allocatedBytesAfter - allocatedBytesBefore} bytes");
                    methodResult = -1;
                }
                else if ((expectedAllocationsKind == AllocationKind.Heap) && (allocatedBytesBefore == allocatedBytesAfter))
                {
                    Console.WriteLine($"FAILURE ({methodName}): unexpected stack allocation");
                    methodResult = -1;
                }
                else
                {
                    Console.WriteLine($"SUCCESS ({methodName})");
                }
            }
            catch
            {
                if (throws)
                {
                    Console.WriteLine($"SUCCESS ({methodName})");
                }
                else
                {
                    throw;
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassAndAddFields()
        {
            SimpleClassA a = new SimpleClassA(f1, f2);
            GC.Collect();
            return a.f1 + a.f2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassesAndEQCompareThem()
        {
            SimpleClassA a1 = new SimpleClassA(f1, f2);
            SimpleClassA a2 = (f1 == 0) ? a1 : new SimpleClassA(f2, f1);
            GC.Collect();
            return (a1 == a2) ? 1 : 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassesAndNECompareThem()
        {
            SimpleClassA a1 = new SimpleClassA(f1, f2);
            SimpleClassA a2 = (f1 == 0) ? a1 : new SimpleClassA(f2, f1);
            GC.Collect();
            return (a1 != a2) ? 1 : 0;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassAndCheckTypeNoHelper()
        {
            object o = (f1 == 0) ? (object)new SimpleClassB(f1, f2) : (object)new SimpleClassA(f1, f2);
            GC.Collect();
            return (o is SimpleClassB) ? 0 : 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassAndCheckTypeHelper()
        {
            object o = (f1 == 0) ? (object)new SimpleClassB(f1, f2) : (object)new SimpleClassA(f1, f2);
            GC.Collect();
            return !(o is SimpleClassA) ? 0 : 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassAndCast()
        {
            object o = (f1 == 0) ? (object)new SimpleClassB(f1, f2) : (object)new SimpleClassA(f2, f1);
            GC.Collect();
            return ((SimpleClassA)o).f1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassAndGetField()
        {
            SimpleClassA a = new SimpleClassA(f1, f2);
            GC.Collect();
            ref int f = ref a.f2;
            return f;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateClassWithNestedStructAndGetField()
        {
            ClassWithNestedStruct c = new ClassWithNestedStruct(f1, f2);
            GC.Collect();
            ref int f = ref c.ns.s.f1;
            return f;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateClassWithNestedStructAndAddFields()
        {
            ClassWithNestedStruct c = new ClassWithNestedStruct(f1, f2);
            GC.Collect();
            return c.ns.f1 + c.ns.f2 + c.ns.s.f1 + c.ns.s.f2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateSimpleClassWithGCFieldAndAddFields()
        {
            SimpleClassWithGCField c = new SimpleClassWithGCField(f1, f2, null);
            GC.Collect();
            return c.f1 + c.f2;
        }

        static int AllocateSimpleClassAndAssignRefToAField()
        {
            SimpleClassWithGCField c = new SimpleClassWithGCField(f1, f2, null);
            GC.Collect();
            c.o = classA;
            return c.f1 + c.f2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int BoxSimpleStructAndAddFields()
        {
            SimpleStruct str;
            str.f1 = f1;
            str.f2 = f2;
            object boxedSimpleStruct = (object)str;
            GC.Collect();
            return ((SimpleStruct)boxedSimpleStruct).f1 + ((SimpleStruct)boxedSimpleStruct).f2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int TestMixOfReportingAndWriteBarriers()
        {
            // c1 doesn't escape and is allocated on the stack
            SimpleClassWithGCField c1 = new SimpleClassWithGCField(f1, f2, str0);

            // c2 always points to a heap-allocated object
            SimpleClassWithGCField c2 = classWithGCField;

            // c2 and c3 may point to a heap-allocated object or to a stack-allocated object
            SimpleClassWithGCField c3 = (f1 == 0) ? c1 : c2;
            SimpleClassWithGCField c4 = (f2 == 0) ? c2 : c1;

            // c1 doesn't have to be reported to GC (but can be conservatively reported as an interior pointer)
            // c1.o should be reported to GC as a normal pointer (but can be conservatively reported as an interior pointer)
            // c2 should be reported to GC as a normal pointer (but can be conservatively reported as an interior pointer)
            // c3 and c4 must be reported as interior pointers
            GC.Collect();

            // This assignment doesn't need a write barrier but may conservatively use a checked barrier
            c1.o = str1;
            // This assignment should optimally use a normal write barrier but may conservatively use a checked barrier
            c2.o = str2;
            // These assignments require a checked write barrier
            c3.o = str3;
            c4.o = str4;

            return c1.o.ToString().Length + c2.o.ToString().Length + c3.o.ToString().Length + c4.o.ToString().Length;
        }

        static int AllocateClassWithGcFieldAndInt()
        {
            ClassWithGCFieldAndInt c = new ClassWithGCFieldAndInt(f1, null);
            GC.Collect();
            return c.i;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateArrayWithNonGCElements()
        {
            int[] array = new int[42];
            array[24] = 42;
            GC.Collect();
            return array[24] + array.Length;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateArrayWithGCElements()
        {
            string[] array = new string[42];
            array[24] = "42";
            GC.Collect();
            return array[24].Length * 21 + array.Length;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateArrayT<T>()
        {
            T[] array = new T[42];
            T t = array[24];
            GC.Collect();

            // Todo -- validate array type (currently causes escape for shared)
            // Todo -- store to array (currently causes escape for shared)

            Consume(t);
            return array.Length + 42;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SpanCaptureArray1()
        {
            Span<int> span = new int[100];
            span[10] = 41;
            return span[10] + span[42];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SpanCaptureArray2() => SpanCaptureArray2Helper(null);

        static int SpanCaptureArray2Helper(int[]? x)
        {
            Span<int> span = x ?? new int[100];
            span[10] = 25;
            return span[10] + span[42];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SpanCaptureArray3()
        {
            Span<int> span = new int[128];
            span[10] = 100;
            Span<int> x = span;
            return x[10] + span[42];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SpanCaptureArrayT<T>()
        {
            Span<T> span = new T[37];
            Use<T>(span[0]);
            return span.Length;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SpanEscapeArrayArg()
        {
            Span<int> y = new int[100];
            Use(y);
            TrashStack();
            return y[42];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SpanEscapeArrayArgCopy()
        {
            Span<int> x = new int[100];
            Span<int> y = x;
            Use(y);
            TrashStack();
            return y[42];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SpanEscapeArrayReturn() => SpanEscapeArrayReturnHelper()[10];

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Span<int> SpanEscapeArrayReturnHelper()
        {
            Span<int> x = new int[44];
            Span<int> y = x;
            x[10] = 99;
            return y;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SpanEscapeArrayOutParam()
        {
            Span<int> x;
            SpanEscapeArrayOutParamHelper(out x);
            TrashStack();
            return x[10];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void SpanEscapeArrayOutParamHelper(out Span<int> a)
        {
            a = new Span<int>(new int[44]);
            a[10] = 22;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SpanEscapeArrayOutParam2()
        {
            SpanKeeper<int> y;
            SpanEscapeArrayOutParam2Helper(out y);
            TrashStack();
            return y.span[10];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void SpanEscapeArrayOutParam2Helper(out SpanKeeper<int> b)
        {
            int[] x = new int[44];
            x[10] = 22;
            b.span = x;
            b.a = 1;
            b.b = 2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SpanEscapeRef()
        {
            ref int q = ref SpanEscapeRef(55);
            TrashStack();
            return q;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ref int SpanEscapeRef(int n)
        {
            Span<int> x = new int[100];
            x[99] = n;
            return ref x[99];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateArrayWithNonGCElementsEscape()
        {
            int[] array = new int[42];
            Use(ref array[24]);
            GC.Collect();
            return array[24];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateArrayWithGCElementsEscape()
        {
            string[] array = new string[42];
            Use(ref array[24]);
            GC.Collect();
            return array[24].Length * 21;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateArrayWithNonGCElementsOutOfRangeRight()
        {
            int[] array = new int[42];
            array[43] = 42;
            GC.Collect();
            return 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateArrayWithNonGCElementsOutOfRangeLeft()
        {
            int[] array = new int[42];
            array[-1] = 42;
            GC.Collect();
            return 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateNegativeLengthArrayWithNonGCElements()
        {
            int[] array = new int["".Length - 2];
            GC.Collect();
            return 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int AllocateLongLengthArrayWithNonGCElements()
        {
            int[] array = new int[long.MaxValue];
            GC.Collect();
            return 1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int StructReferredObjects()
        {
            int[] a1 = new int[10];
            int[] a2 = new int[10];

            a1[3] = 7;
            a2[4] = 8;

            GCStruct s = new GCStruct() { i = 10, o1 = a1, o2 = a2 };

            return s.i + s.o1[3] + s.o2[4];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int StructReferredObjectEscape1() => StructReferredObjectEscape1Helper()[3];

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int[] StructReferredObjectEscape1Helper()
        {
            int[] a1 = new int[10];
            int[] a2 = a1;

            a1[3] = 33;
            a2[4] = 8;

            GCStruct s = new GCStruct() { i = 10, o1 = a1, o2 = a2 };

            return s.o2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int StructReferredObjectEscape2()
        {
            ref int a = ref StructReferredObjectEscape2Helper();
            TrashStack();
            return a;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static ref int StructReferredObjectEscape2Helper()
        {
            int[] a1 = new int[10];
            int[] a2 = a1;

            a1[3] = 33;
            a2[4] = 8;

            GCStruct s = new GCStruct() { i = 10, o1 = a1, o2 = a2 };
            return ref s.o2[3];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int StructReferredObjectEscape3()
        {
            GCStruct s = StructReferredObjectEscape3Helper();
            return s.o1[3] + s.o2[4];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static GCStruct StructReferredObjectEscape3Helper()
        {
            int[] a1 = new int[10];
            int[] a2 = a1;

            a1[3] = 33;
            a2[4] = 8;

            GCStruct s = new GCStruct() { i = 10, o1 = a1, o2 = a2 };
            return s;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int StructReferredObjectEscape4()
        {
            GCStruct s;
            StructReferredObjectEscape4Helper(out s);
            return s.o1[3] + s.o2[4];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void StructReferredObjectEscape4Helper(out GCStruct s)
        {
            int[] a1 = new int[10];
            int[] a2 = a1;

            a1[3] = 33;
            a2[4] = 8;

            s = new GCStruct() { i = 10, o1 = a1, o2 = a2 };
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int StructReferredObjectEscape5()
        {
            string[] s = StructReferredObjectEscape5Helper(0);
            TrashStack();
            return s[0].Length;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string[] StructReferredObjectEscape5Helper(int n)
        {
            GCStruct2 g = new GCStruct2();
            g.a = new string[10];
            g.b = new string[10];

            g.a[0] = "Goodbye";
            g.b[0] = "Hello";

            ref string[] rs = ref g.b;

            if (n > 0)
            {
                rs = ref g.a;
            }

            return rs;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Use(ref int v)
        {
            v = 42;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Use(ref string s)
        {
            s = "42";
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Use(Span<int> span)
        {
            span[42] = 42;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void Use<T>(T t)
        {

        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static Span<int> Identity(Span<int> x) => x;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ZeroAllocTest()
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            Case1();
            EnsureZeroAllocated(before, 1);
            Case2();
            EnsureZeroAllocated(before, 2);
            Case3(null);
            EnsureZeroAllocated(before, 3);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EnsureZeroAllocated(long before, int caseNumber)
        {
            long after = GC.GetAllocatedBytesForCurrentThread();
            if (after - before != 0)
                throw new InvalidOperationException($"Unexpected allocation in Case {caseNumber}: {after - before} bytes");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long Case1()
        {
            // Explicit object allocation, but the object
            // never escapes the method.
            MyRecord obj = new MyRecord(1, 2, default);
            return obj.A + obj.B;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Case2()
        {
            // Box it
            object o = new Guid();
            Consume(42);
            // Unbox it (multi-use)
            Consume((Guid)o);
            Consume((Guid)o);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Case3(object? o)
        {
            // A condition to make it more complicated
            // (and trigger CORINFO_HELP_UNBOX_TYPETEST)
            if (o == null)
            {
                // Box it
                o = new Guid();
            }
            // Unbox it
            Consume((Guid)o);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Consume<T>(T _)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void TrashStack()
        {
            Span<int> span = stackalloc int[128];
            for (int i = 0; i < span.Length; i++)
            {
                span[i] = -1;
            }
        }

        private record class MyRecord(int A, long B, Guid C);
    }
}
