using System;
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Remote.Neeo
{
    public static class JsonSerialization
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions(JsonSerializerDefaults.Web).ApplySettings();

        public static JsonSerializerOptions ApplySettings(this JsonSerializerOptions options)
        {
            options.DictionaryKeyPolicy = options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
            return options;
        }

        public static T ToObject<T>(this JsonElement element) => JsonSerializer.Deserialize<T>(element.GetSpan(), JsonSerialization.Options)!;

        public static object ToObject(this JsonElement element, Type returnType) => JsonSerializer.Deserialize(element.GetSpan(), returnType, JsonSerialization.Options)!;

        public static T ToObject<T>(this JsonDocument document) => (document ?? throw new ArgumentNullException(nameof(document))).RootElement.ToObject<T>();

        public static object ToObject(this JsonDocument document, Type returnType) => (document ?? throw new ArgumentNullException(nameof(document))).RootElement.ToObject(returnType);

        private static ReadOnlySpan<byte> GetSpan(this JsonElement element)
        {
            ArrayBufferWriter<byte> bufferWriter = new();
            using (Utf8JsonWriter writer = new(bufferWriter))
            {
                element.WriteTo(writer);
            }
            return bufferWriter.WrittenSpan;
        }
    }
}
