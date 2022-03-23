using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Converts enumerated values to/from their respective text in Json serialization.
/// <para/>
/// See <see cref="TextAttribute"/> for more details.
/// </summary>
/// <typeparam name="TValue">The type of the enumerated value.</typeparam>
public sealed class TextJsonConverter<TValue> : JsonConverter<TValue>
    where TValue : struct, Enum
{
    /// <summary>
    /// Reads the next <typeparamref name="TValue" /> from the JSON reader.
    /// </summary>
    public override TValue Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options) => reader.GetString() is { } text && TextAttribute.GetEnum<TValue>(text) is { } value
        ? value
        : default;

    /// <summary>
    /// Writes the <paramref name="value" /> to the JSON writer.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, TValue value, JsonSerializerOptions options) => writer.WriteStringValue(TextAttribute.GetText(value));
}