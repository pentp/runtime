// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    public static partial class JsonSerializer
    {
        /// <summary>
        /// Converts the <see cref="JsonDocument"/> representing a single JSON value into a <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The type to deserialize the JSON value into.</typeparam>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="document">The <see cref="JsonDocument"/> to convert.</param>
        /// <param name="options">Options to control the behavior during parsing.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="document"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// <typeparamref name="TValue" /> is not compatible with the JSON.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <typeparamref name="TValue"/> or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static TValue? Deserialize<TValue>(this JsonDocument document, JsonSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(document);

            JsonTypeInfo<TValue> jsonTypeInfo = GetTypeInfo<TValue>(options);
            ReadOnlySpan<byte> utf8Json = document.GetRootRawValue().Span;
            return ReadFromSpan(utf8Json, jsonTypeInfo);
        }

        /// <summary>
        /// Converts the <see cref="JsonDocument"/> representing a single JSON value into a <paramref name="returnType"/>.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="document">The <see cref="JsonDocument"/> to convert.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="options">Options to control the behavior during parsing.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="document"/> or <paramref name="returnType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// <paramref name="returnType"/> is not compatible with the JSON.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="returnType"/> or its serializable members.
        /// </exception>
        [RequiresUnreferencedCode(SerializationUnreferencedCodeMessage)]
        [RequiresDynamicCode(SerializationRequiresDynamicCodeMessage)]
        public static object? Deserialize(this JsonDocument document, Type returnType, JsonSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(returnType);

            JsonTypeInfo jsonTypeInfo = GetTypeInfo(options, returnType);
            ReadOnlySpan<byte> utf8Json = document.GetRootRawValue().Span;
            return ReadFromSpanAsObject(utf8Json, jsonTypeInfo);
        }

        /// <summary>
        /// Converts the <see cref="JsonDocument"/> representing a single JSON value into a <typeparamref name="TValue"/>.
        /// </summary>
        /// <typeparam name="TValue">The type to deserialize the JSON value into.</typeparam>
        /// <returns>A <typeparamref name="TValue"/> representation of the JSON value.</returns>
        /// <param name="document">The <see cref="JsonDocument"/> to convert.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="document"/> is <see langword="null"/>.
        ///
        /// -or-
        ///
        /// <paramref name="jsonTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// <typeparamref name="TValue" /> is not compatible with the JSON.
        /// </exception>
        public static TValue? Deserialize<TValue>(this JsonDocument document, JsonTypeInfo<TValue> jsonTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(jsonTypeInfo);

            jsonTypeInfo.EnsureConfigured();
            ReadOnlySpan<byte> utf8Json = document.GetRootRawValue().Span;
            return ReadFromSpan(utf8Json, jsonTypeInfo);
        }

        /// <summary>
        /// Converts the <see cref="JsonDocument"/> representing a single JSON value into an instance specified by the <paramref name="jsonTypeInfo"/>.
        /// </summary>
        /// <param name="document">The <see cref="JsonDocument"/> to convert.</param>
        /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
        /// <returns>A <paramref name="jsonTypeInfo"/> representation of the JSON value.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="document"/> is <see langword="null"/>.
        ///
        /// -or-
        ///
        /// <paramref name="jsonTypeInfo"/> is <see langword="null"/>.
        /// </exception>
        public static object? Deserialize(this JsonDocument document, JsonTypeInfo jsonTypeInfo)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(jsonTypeInfo);

            jsonTypeInfo.EnsureConfigured();
            ReadOnlySpan<byte> utf8Json = document.GetRootRawValue().Span;
            return ReadFromSpanAsObject(utf8Json, jsonTypeInfo);
        }

        /// <summary>
        /// Converts the <see cref="JsonDocument"/> representing a single JSON value into a <paramref name="returnType"/>.
        /// </summary>
        /// <returns>A <paramref name="returnType"/> representation of the JSON value.</returns>
        /// <param name="document">The <see cref="JsonDocument"/> to convert.</param>
        /// <param name="returnType">The type of the object to convert to and return.</param>
        /// <param name="context">A metadata provider for serializable types.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="document"/> is <see langword="null"/>.
        ///
        /// -or-
        ///
        /// <paramref name="returnType"/> is <see langword="null"/>.
        ///
        /// -or-
        ///
        /// <paramref name="context"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="JsonException">
        /// The JSON is invalid.
        ///
        /// -or-
        ///
        /// <paramref name="returnType" /> is not compatible with the JSON.
        ///
        /// -or-
        ///
        /// There is remaining data in the string beyond a single JSON value.</exception>
        /// <exception cref="NotSupportedException">
        /// There is no compatible <see cref="System.Text.Json.Serialization.JsonConverter"/>
        /// for <paramref name="returnType"/> or its serializable members.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The <see cref="JsonSerializerContext.GetTypeInfo(Type)"/> method of the provided
        /// <paramref name="context"/> returns <see langword="null"/> for the type to convert.
        /// </exception>
        public static object? Deserialize(this JsonDocument document, Type returnType, JsonSerializerContext context)
        {
            ArgumentNullException.ThrowIfNull(document);
            ArgumentNullException.ThrowIfNull(returnType);
            ArgumentNullException.ThrowIfNull(context);

            JsonTypeInfo jsonTypeInfo = GetTypeInfo(context, returnType);
            ReadOnlySpan<byte> utf8Json = document.GetRootRawValue().Span;
            return ReadFromSpanAsObject(utf8Json, jsonTypeInfo);
        }
    }
}
