// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Mono.Cecil;

namespace Mono.Linker.Dataflow
{
    // This represents a field which has been generated by the compiler as the
    // storage location for a hoisted local (a local variable which is lifted to a
    // field on a state machine type, or to a field on a closure accessed by lambdas
    // or local functions).
    public readonly struct HoistedLocalKey : IEquatable<HoistedLocalKey>
    {
        readonly FieldReference Field;

        public HoistedLocalKey(FieldReference field)
        {
            Debug.Assert(CompilerGeneratedState.IsHoistedLocal(field));
            Field = field;
        }

        public bool Equals(HoistedLocalKey other) => Field.Equals(other.Field);

        public override bool Equals(object? obj) => obj is HoistedLocalKey other && Equals(other);

        public override int GetHashCode() => Field.GetHashCode();

        public static bool operator ==(HoistedLocalKey left, HoistedLocalKey right) => left.Equals(right);
        public static bool operator !=(HoistedLocalKey left, HoistedLocalKey right) => !(left == right);
    }
}
