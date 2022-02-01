using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Json;

/// <summary>
/// Specifies that objects of a specified base type should directly be serialized as their implemented types.
/// </summary>
internal sealed class JsonDirectSerializationAttribute : JsonConverterAttribute
{
    /// <summary>
    /// Creates a new instance of the <see cref="JsonDirectSerializationAttribute" />.
    /// </summary>
    /// <param name="baseType">
    /// A type for which to create a JSON converter, such that objects of this type should directly be serialized as their implemented types.
    /// </param>
    public JsonDirectSerializationAttribute(Type baseType)
        : base(typeof(Converter<>).MakeGenericType(baseType))
    {
    }

    private sealed class Converter<T> : JsonConverter<T>
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => JsonSerializer.Serialize(
            writer,
            value,
            value?.GetType() ?? typeof(object),
            options
        );
    }
}