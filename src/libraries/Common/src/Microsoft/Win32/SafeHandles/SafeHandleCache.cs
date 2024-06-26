// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>Provides a cache for special instances of SafeHandles.</summary>
    /// <typeparam name="T">Specifies the type of SafeHandle.</typeparam>
    internal static class SafeHandleCache<T> where T : SafeHandle
    {
        private static T? s_invalidHandle;

        /// <summary>
        /// Gets a cached, invalid handle.  As the instance is cached, it should either never be Disposed
        /// or it should override <see cref="SafeHandle.Dispose(bool)"/> to prevent disposal when the
        /// instance represents an invalid handle: <see cref="System.Runtime.InteropServices.SafeHandle.IsInvalid"/> returns <see language="true"/>.
        /// </summary>
        internal static T GetInvalidHandle(Func<T> invalidHandleFactory)
        {
            return s_invalidHandle ?? CreateInvalidHandle(invalidHandleFactory);

            static T CreateInvalidHandle(Func<T> invalidHandleFactory)
            {
                T newHandle = invalidHandleFactory();
                T? currentHandle = Interlocked.CompareExchange(ref s_invalidHandle, newHandle, null);

                if (currentHandle == null)
                {
                    GC.SuppressFinalize(newHandle);
                    currentHandle = newHandle;
                }
                else
                {
                    newHandle.Dispose();
                }

                Debug.Assert(currentHandle.IsInvalid);
                return currentHandle;
            }
        }

        /// <summary>Gets whether the specified handle is invalid handle.</summary>
        /// <param name="handle">The handle to compare.</param>
        /// <returns>true if <paramref name="handle"/> is invalid handle; otherwise, false.</returns>
        internal static bool IsCachedInvalidHandle(SafeHandle handle)
        {
            Debug.Assert(handle != null);
            bool isCachedInvalidHandle = ReferenceEquals(handle, s_invalidHandle);
            Debug.Assert(!isCachedInvalidHandle || handle.IsInvalid, "The cached invalid handle must still be invalid.");
            return isCachedInvalidHandle;
        }
    }
}
