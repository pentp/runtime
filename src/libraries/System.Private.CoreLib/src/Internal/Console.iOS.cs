// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Internal
{
    public static partial class Console
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static unsafe void Write(string s)
        {
            fixed (char* ptr = s)
            {
                Interop.Sys.Log((byte*)ptr, s.Length * 2);
            }
        }
        public static partial class Error
        {
            [MethodImplAttribute(MethodImplOptions.NoInlining)]
            public static unsafe void Write(string s)
            {
                fixed (char* ptr = s)
                {
                    Interop.Sys.LogError((byte*)ptr, s.Length * 2);
                }
            }
        }
    }
}
