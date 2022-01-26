using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Json;

internal sealed class JsonInterfaceSerializationConverterAttribute : JsonConverterAttribute
{
    public JsonInterfaceSerializationConverterAttribute(Type interfaceType)
        : base(typeof(Converter<>).MakeGenericType(interfaceType))
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
