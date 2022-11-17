using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Specifies that objects of a specified base type should directly be serialized as their implemented types.
/// </summary>
/// <typeparam name="T">
/// A type for which to create a JSON converter, such that objects of this type should directly be serialized as their implemented types.
/// </typeparam>
internal sealed class JsonDirectSerializationAttribute<T> : JsonConverterAttribute
    where T : notnull
{
    /// <summary>
    /// Creates a new instance of the <see cref="JsonDirectSerializationAttribute&lt;T&gt;" />.
    /// </summary>
    public JsonDirectSerializationAttribute()
        : base(typeof(Converter))
    {
    }

    private sealed class Converter : JsonConverter<T>
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => JsonSerializer.Serialize(
            writer,
            value,
            value.GetType(),
            options
        );
    }
}