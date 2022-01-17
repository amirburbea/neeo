using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Api.Json;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
internal sealed class JsonInterfaceConverterAttribute : JsonConverterAttribute
{
    public JsonInterfaceConverterAttribute(Type interfaceType)
        : base(JsonInterfaceConverterAttribute.GetConverterType(interfaceType))
    {
    }

    private static Type GetConverterType(Type interfaceType)
    {
        return !(interfaceType ?? throw new ArgumentNullException(nameof(interfaceType))).IsInterface
            ? throw new ArgumentException($"{interfaceType.FullName} is not an interface.", nameof(interfaceType))
            : typeof(ObjectConverter<>).MakeGenericType(interfaceType);
    }

    private sealed class ObjectConverter<T> : JsonConverter<T>
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotSupportedException();
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value?.GetType() ?? typeof(object), options);
        }
    }
}
