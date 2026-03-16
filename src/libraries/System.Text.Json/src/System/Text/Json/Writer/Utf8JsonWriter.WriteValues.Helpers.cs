// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Text.Json
{
    public sealed partial class Utf8JsonWriter
    {
        /// <summary>
        /// Returns whether a JSON value can be written at the current position based on the current <see cref="_enclosingContainer"/>:
        /// <list type="bullet">
        /// <item>
        /// <see cref="EnclosingContainerType.Array"/>: Writing a value is always allowed.
        /// Because <see cref="EnclosingContainerType.Array"/> is negative when cast to <see cref="sbyte"/>, it overrides the equality checks below.
        /// </item>
        /// <item>
        /// <see cref="EnclosingContainerType.Object"/>: Writing a value is allowed only if <see cref="_tokenType"/> is a property name.
        /// Because we designed <see cref="EnclosingContainerType.Object"/> == <see cref="JsonTokenType.PropertyName"/>, we can just check for equality.
        /// </item>
        /// <item>
        /// <see cref="EnclosingContainerType.None"/>: Writing a value is allowed only if <see cref="_tokenType"/> is None (only one value may be written at the root).
        /// This case is identical to the previous case.
        /// </item>
        /// <item>
        /// <see cref="EnclosingContainerType.Utf8StringSequence"/>, <see cref="EnclosingContainerType.Utf16StringSequence"/>, <see cref="EnclosingContainerType.Base64StringSequence"/>:
        /// Writing a value is never valid and <see cref="_enclosingContainer"/> does not equal any <see cref="JsonTokenType"/> by construction.
        /// </item>
        /// </list>
        /// </summary>
        private bool CanWriteValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ((sbyte)_enclosingContainer ^ (byte)_tokenType) <= 0;
        }

        private bool HasPartialStringData => _partialStringDataLength != 0;

        private void ClearPartialStringData() => _partialStringDataLength = 0;

        private void ValidateWritingValue()
        {
            if (!CanWriteValue)
            {
                OnValidateWritingValueFailed();
            }
        }

        [DoesNotReturn]
        private void OnValidateWritingValueFailed()
        {
            Debug.Assert(!_options.SkipValidation);

            if (IsWritingPartialString)
            {
                ThrowInvalidOperationException_CannotWriteWithinString();
            }

            throw GetValidateWritingValueFailedException();
        }

        private InvalidOperationException GetValidateWritingValueFailedException()
        {
            Debug.Assert(!HasPartialStringData);

            string message;
            if (_enclosingContainer == EnclosingContainerType.Object)
            {
                Debug.Assert(_tokenType != JsonTokenType.PropertyName);
                Debug.Assert(_tokenType != JsonTokenType.None && _tokenType != JsonTokenType.StartArray);
                message = SR.CannotWriteValueWithinObject;
            }
            else
            {
                Debug.Assert(_tokenType != JsonTokenType.PropertyName);
                Debug.Assert(CurrentDepth == 0 && _tokenType != JsonTokenType.None);
                message = SR.CannotWriteValueAfterPrimitiveOrClose;
            }
            return ThrowHelper.GetInvalidOperationException(SR.Format(message, _tokenType));
        }

        [DoesNotReturn]
        private void OnValidateWritingSegmentFailed(EnclosingContainerType currentSegmentEncoding)
            => throw GetValidateWritingSegmentFailedException(currentSegmentEncoding);

        private InvalidOperationException GetValidateWritingSegmentFailedException(EnclosingContainerType currentSegmentEncoding)
        {
            if (IsWritingPartialString)
            {
                return ThrowHelper.GetInvalidOperationException(SR.Format(SR.CannotMixEncodings, GetEncodingName(_enclosingContainer), GetEncodingName(currentSegmentEncoding)));

                static string GetEncodingName(EnclosingContainerType encoding)
                {
                    switch (encoding)
                    {
                        case EnclosingContainerType.Utf8StringSequence: return "UTF-8";
                        case EnclosingContainerType.Utf16StringSequence: return "UTF-16";
                        case EnclosingContainerType.Base64StringSequence: return "Base64";
                        default:
                            Debug.Fail("Unknown encoding.");
                            return "Unknown";
                    }
                }
            }

            return GetValidateWritingValueFailedException();
        }

        private void ValidateNotWithinUnfinalizedString()
        {
            if (IsWritingPartialString)
            {
                ThrowInvalidOperationException_CannotWriteWithinString();
            }

            Debug.Assert(!HasPartialStringData);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Base64EncodeAndWrite(ReadOnlySpan<byte> bytes, Span<byte> output)
        {
            Span<byte> destination = output.Slice(BytesPending);
            OperationStatus status = Base64.EncodeToUtf8(bytes, destination, out int consumed, out int written);
            Debug.Assert(status == OperationStatus.Done);
            Debug.Assert(consumed == bytes.Length);
            BytesPending += written;
        }
    }
}
