// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class AesGcm
    {
        private FixedMemoryKeyBox _keyBox;

        // CryptoKit only supports 16 byte tags.
        private static readonly KeySizes s_tagByteSizes = new KeySizes(16, 16, 1);

        // CryptoKit added AES.GCM in macOS 10.15, which is lower than our minimum target for macOS/MacCatalyst. On iOS/tvOS, it was added in 13.0.
        public static partial bool IsSupported =>
            OperatingSystem.IsMacOS() ||
            OperatingSystem.IsMacCatalyst() ||
            OperatingSystem.IsIOSVersionAtLeast(13) ||
            OperatingSystem.IsTvOSVersionAtLeast(13);

        public static partial KeySizes TagByteSizes => s_tagByteSizes;

        [MemberNotNull(nameof(_keyBox))]
        private partial void ImportKey(ReadOnlySpan<byte> key)
        {
            // We should only be calling this in the constructor, so there shouldn't be a previous key.
            Debug.Assert(_keyBox is null);
            _keyBox = new FixedMemoryKeyBox(key);
        }

        private partial void EncryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> plaintext,
            Span<byte> ciphertext,
            Span<byte> tag,
            ReadOnlySpan<byte> associatedData)
        {
            bool acquired = false;

            try
            {
                _keyBox.DangerousAddRef(ref acquired);
                Interop.AppleCrypto.AesGcmEncrypt(
                    _keyBox.DangerousKeySpan,
                    nonce,
                    plaintext,
                    ciphertext,
                    tag,
                    associatedData);
            }
            finally
            {
                if (acquired)
                {
                    _keyBox.DangerousRelease();
                }
            }
        }

        private partial void DecryptCore(
            ReadOnlySpan<byte> nonce,
            ReadOnlySpan<byte> ciphertext,
            ReadOnlySpan<byte> tag,
            Span<byte> plaintext,
            ReadOnlySpan<byte> associatedData)
        {
            bool acquired = false;

            try
            {
                _keyBox.DangerousAddRef(ref acquired);
                Interop.AppleCrypto.AesGcmDecrypt(
                    _keyBox.DangerousKeySpan,
                    nonce,
                    ciphertext,
                    tag,
                    plaintext,
                    associatedData);
            }
            finally
            {
                if (acquired)
                {
                    _keyBox.DangerousRelease();
                }
            }
        }

        public partial void Dispose() => _keyBox.Dispose();
    }
}
