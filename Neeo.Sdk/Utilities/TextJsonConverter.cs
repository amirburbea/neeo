using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Converts enumerated values to/from their respective text in Json serialization.
/// Reads and writes flagged enumerations as a comma separated list of flags.
/// <para/>
/// See <see cref="TextAttribute"/> for more details.
/// </summary>
/// <typeparam name="TValue">The type of the enumerated value.</typeparam>
public sealed class TextJsonConverter<TValue> : JsonConverter<TValue>
    where TValue : struct, Enum
{
    private readonly bool _isFlagged = typeof(TValue).GetCustomAttribute<FlagsAttribute>() is { };

    /// <summary>
    /// Reads the next <typeparamref name="TValue" /> from the JSON reader.
    /// </summary>
    public override TValue Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.GetString() is not { } text)
        {
            return default;
        }
        if (!this._isFlagged || !text.Contains(','))
        {
            return TextAttribute.GetEnum<TValue>(text) ?? default;
        }
        return text.Split(',').Aggregate(default(TValue), (value, text) =>
        {
            if (TextAttribute.GetEnum<TValue>(text) is not { } flag)
            {
                return value;
            }
            ulong output = Unsafe.As<TValue, ulong>(ref value) | Unsafe.As<TValue, ulong>(ref flag);
            return Unsafe.As<ulong, TValue>(ref output);
        });
    }

    /// <summary>
    /// Writes the <paramref name="value" /> to the JSON writer.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, TValue value, JsonSerializerOptions options) => writer.WriteStringValue(
        this._isFlagged ? string.Join(',', FlaggedEnumerations.GetNames(value)) : TextAttribute.GetText(value)
    );
}
