using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Json;

internal sealed class JsonDirectSerializationAttribute : JsonConverterAttribute
{
    private readonly Type _interfaceType;
    private readonly JsonConverter _converter;

    public JsonDirectSerializationAttribute(Type interfaceType)
    {
        this._interfaceType = interfaceType;
        this._converter = (JsonConverter)Activator.CreateInstance(typeof(Converter<>).MakeGenericType(interfaceType))!;
    }

    public override JsonConverter? CreateConverter(Type typeToConvert)
    {
        if (this._interfaceType == typeToConvert)
        {
            return this._converter;
        }
        return base.CreateConverter(typeToConvert);
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
