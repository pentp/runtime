// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Text.Encodings.Web;

namespace System.Text.Json
{
    internal static partial class JsonWriterHelper
    {
        // Only allow ASCII characters between ' ' (0x20) and '~' (0x7E), inclusively,
        // but exclude characters that need to be escaped as hex: '"', '\'', '&', '+', '<', '>', '`'
        // and exclude characters that need to be escaped by adding a backslash: '\n', '\r', '\t', '\\', '\b', '\f'
        //
        // non-zero = allowed, 0 = disallowed
        public const int LastAsciiCharacter = 0x7F;
        private static ReadOnlySpan<byte> AllowList => // byte.MaxValue + 1
        [
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // U+0000..U+000F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // U+0010..U+001F
            1, 1, 0, 1, 1, 1, 0, 0, 1, 1, 1, 0, 1, 1, 1, 1, // U+0020..U+002F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 0, 1, // U+0030..U+003F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // U+0040..U+004F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, // U+0050..U+005F
            0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, // U+0060..U+006F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, // U+0070..U+007F

            // Also include the ranges from U+0080 to U+00FF for performance to avoid UTF8 code from checking boundary.
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, // U+00F0..U+00FF
        ];

        private static bool NeedsEscaping(byte value) => AllowList[value] == 0;

        private static bool NeedsEscapingNoBoundsCheck(char value) => AllowList[value] == 0;

        public static int NeedsEscaping(ReadOnlySpan<byte> value, JavaScriptEncoder? encoder)
        {
            return (encoder ?? JavaScriptEncoder.Default).FindFirstCharacterToEncodeUtf8(value);
        }

        public static int NeedsEscaping(ReadOnlySpan<char> value, JavaScriptEncoder? encoder)
        {
            // Some implementations of JavaScriptEncoder.FindFirstCharacterToEncode may not accept
            // null pointers and guard against that. Hence, check up-front to return -1.
            if (value.IsEmpty)
            {
                return -1;
            }

            // Unfortunately, there is no public API for FindFirstCharacterToEncode(Span<char>) yet,
            // so we have to use the unsafe FindFirstCharacterToEncode(char*, int) instead.
            unsafe
            {
                fixed (char* ptr = value)
                {
                    return (encoder ?? JavaScriptEncoder.Default).FindFirstCharacterToEncode(ptr, value.Length);
                }
            }
        }

        public static int GetMaxEscapedLength(int textLength, int firstIndexToEscape)
        {
            Debug.Assert(textLength > 0);
            Debug.Assert(firstIndexToEscape >= 0 && firstIndexToEscape < textLength);
            return firstIndexToEscape + JsonConstants.MaxExpansionFactorWhileEscaping * (textLength - firstIndexToEscape);
        }

        private static int EscapeString(ReadOnlySpan<byte> value, Span<byte> destination, JavaScriptEncoder encoder, ref int consumed, int written, bool isFinalBlock)
        {
            Debug.Assert(encoder != null);

            OperationStatus result = encoder.EncodeUtf8(value, destination, out int encoderBytesConsumed, out int encoderBytesWritten, isFinalBlock);

            Debug.Assert(result != OperationStatus.DestinationTooSmall);
            Debug.Assert(result != OperationStatus.NeedMoreData || !isFinalBlock);

            if (!(result == OperationStatus.Done || (result == OperationStatus.NeedMoreData && !isFinalBlock)))
            {
                ThrowHelper.ThrowArgumentException_InvalidUTF8(value.Slice(encoderBytesWritten));
            }

            Debug.Assert(encoderBytesConsumed == value.Length || (result == OperationStatus.NeedMoreData && !isFinalBlock));

            consumed += encoderBytesConsumed;
            return written + encoderBytesWritten;
        }

        public static void EscapeString(ReadOnlySpan<byte> value, Span<byte> destination, int indexOfFirstByteToEscape, JavaScriptEncoder? encoder, out int written)
            => written = EscapeString(value, destination, indexOfFirstByteToEscape, encoder, out _, isFinalBlock: true);

        public static int EscapeString(ReadOnlySpan<byte> value, Span<byte> destination, int indexOfFirstByteToEscape, JavaScriptEncoder? encoder, out int consumed, bool isFinalBlock = true)
        {
            Debug.Assert(indexOfFirstByteToEscape >= 0 && indexOfFirstByteToEscape < value.Length);

            value.Slice(0, indexOfFirstByteToEscape).CopyTo(destination);
            int written = indexOfFirstByteToEscape;

            if (encoder != null)
            {
                destination = destination.Slice(indexOfFirstByteToEscape);
                value = value.Slice(indexOfFirstByteToEscape);
                consumed = indexOfFirstByteToEscape;
                return EscapeString(value, destination, encoder, ref consumed, written, isFinalBlock);
            }
            else
            {
                // For performance when no encoder is specified, perform escaping here for Ascii and on the
                // first occurrence of a non-Ascii character, then call into the default encoder.
                for (; (uint)indexOfFirstByteToEscape < (uint)value.Length; indexOfFirstByteToEscape++)
                {
                    byte val = value[indexOfFirstByteToEscape];
                    if (!NeedsEscaping(val))
                    {
                        destination[written++] = val;
                    }
                    else if (IsAsciiValue(val))
                    {
                        written += EscapeNextBytes(val, destination, written);
                    }
                    else
                    {
                        // Fall back to default encoder.
                        destination = destination.Slice(written);
                        value = value.Slice(indexOfFirstByteToEscape);
                        consumed = indexOfFirstByteToEscape;
                        return EscapeString(value, destination, JavaScriptEncoder.Default, ref consumed, written, isFinalBlock);
                    }
                }
                consumed = indexOfFirstByteToEscape;
                return written;
            }
        }

        private static int EscapeNextBytes(byte value, Span<byte> destination, int written)
        {
            destination = destination.Slice(written, 6);
            switch (value)
            {
                case JsonConstants.Quote:
                    // Optimize for the common quote case.
                    "\\u0022"u8.CopyTo(destination);
                    return 6;
                case JsonConstants.LineFeed:
                    "\\n"u8.CopyTo(destination);
                    return 2;
                case JsonConstants.CarriageReturn:
                    "\\r"u8.CopyTo(destination);
                    return 2;
                case JsonConstants.Tab:
                    "\\t"u8.CopyTo(destination);
                    return 2;
                case JsonConstants.BackSlash:
                    "\\\\"u8.CopyTo(destination);
                    return 2;
                case JsonConstants.BackSpace:
                    "\\b"u8.CopyTo(destination);
                    return 2;
                case JsonConstants.FormFeed:
                    "\\f"u8.CopyTo(destination);
                    return 2;
                default:
                    "\\u00"u8.CopyTo(destination);
                    HexConverter.ToBytesBuffer(value, destination, 4);
                    return 6;
            }
        }

        private static bool IsAsciiValue(byte value) => value <= LastAsciiCharacter;

        private static bool IsAsciiValue(char value) => value <= LastAsciiCharacter;

        private static int EscapeString(ReadOnlySpan<char> value, Span<char> destination, JavaScriptEncoder encoder, ref int consumed, int written, bool isFinalBlock)
        {
            Debug.Assert(encoder != null);

            OperationStatus result = encoder.Encode(value, destination, out int encoderBytesConsumed, out int encoderCharsWritten, isFinalBlock);

            Debug.Assert(result != OperationStatus.DestinationTooSmall);
            Debug.Assert(result != OperationStatus.NeedMoreData || !isFinalBlock);

            if (!(result == OperationStatus.Done || (result == OperationStatus.NeedMoreData && !isFinalBlock)))
            {
                ThrowHelper.ThrowArgumentException_InvalidUTF16(value[encoderCharsWritten]);
            }

            Debug.Assert(encoderBytesConsumed == value.Length || (result == OperationStatus.NeedMoreData && !isFinalBlock));

            consumed += encoderBytesConsumed;
            return written + encoderCharsWritten;
        }

        public static void EscapeString(ReadOnlySpan<char> value, Span<char> destination, int indexOfFirstByteToEscape, JavaScriptEncoder? encoder, out int written)
            => written = EscapeString(value, destination, indexOfFirstByteToEscape, encoder, out _, isFinalBlock: true);

        public static int EscapeString(ReadOnlySpan<char> value, Span<char> destination, int indexOfFirstByteToEscape, JavaScriptEncoder? encoder, out int consumed, bool isFinalBlock = true)
        {
            Debug.Assert(indexOfFirstByteToEscape >= 0 && indexOfFirstByteToEscape < value.Length);

            value.Slice(0, indexOfFirstByteToEscape).CopyTo(destination);
            int written = indexOfFirstByteToEscape;

            if (encoder != null)
            {
                destination = destination.Slice(indexOfFirstByteToEscape);
                value = value.Slice(indexOfFirstByteToEscape);
                consumed = indexOfFirstByteToEscape;
                return EscapeString(value, destination, encoder, ref consumed, written, isFinalBlock);
            }
            else
            {
                // For performance when no encoder is specified, perform escaping here for Ascii and on the
                // first occurrence of a non-Ascii character, then call into the default encoder.
                for (; (uint)indexOfFirstByteToEscape < (uint)value.Length; indexOfFirstByteToEscape++)
                {
                    char val = value[indexOfFirstByteToEscape];
                    if (IsAsciiValue(val))
                    {
                        if (NeedsEscapingNoBoundsCheck(val))
                        {
                            written += EscapeNextChars(val, destination, written);
                        }
                        else
                        {
                            destination[written++] = val;
                        }
                    }
                    else
                    {
                        // Fall back to default encoder.
                        destination = destination.Slice(written);
                        value = value.Slice(indexOfFirstByteToEscape);
                        consumed = indexOfFirstByteToEscape;
                        return EscapeString(value, destination, JavaScriptEncoder.Default, ref consumed, written, isFinalBlock);
                    }
                }
                consumed = indexOfFirstByteToEscape;
                return written;
            }
        }

        private static int EscapeNextChars(char value, Span<char> destination, int written)
        {
            Debug.Assert(IsAsciiValue(value));

            destination = destination.Slice(written, 6);
            switch ((byte)value)
            {
                case JsonConstants.Quote:
                    // Optimize for the common quote case.
                    "\\u0022".AsSpan().CopyTo(destination);
                    return 6;
                case JsonConstants.LineFeed:
                    "\\n".AsSpan().CopyTo(destination);
                    return 2;
                case JsonConstants.CarriageReturn:
                    "\\r".AsSpan().CopyTo(destination);
                    return 2;
                case JsonConstants.Tab:
                    "\\t".AsSpan().CopyTo(destination);
                    return 2;
                case JsonConstants.BackSlash:
                    "\\\\".AsSpan().CopyTo(destination);
                    return 2;
                case JsonConstants.BackSpace:
                    "\\b".AsSpan().CopyTo(destination);
                    return 2;
                case JsonConstants.FormFeed:
                    "\\f".AsSpan().CopyTo(destination);
                    return 2;
                default:
                    "\\u00".AsSpan().CopyTo(destination);
                    HexConverter.ToCharsBuffer((byte)value, destination, 4);
                    return 6;
            }
        }
    }
}
