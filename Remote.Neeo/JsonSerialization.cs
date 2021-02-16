﻿using System;
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Remote.Neeo
{
    /// <summary>
    /// Contains <see langword="static"/> methods and fields related to JSON serialization.
    /// </summary>
    public static class JsonSerialization
    {
        /// <summary>
        /// An instance of <see cref="JsonSerializerOptions"/> suitable for interactions with the NEEO brain.
        /// </summary>
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions(JsonSerializerDefaults.Web).ApplyOptions();

        /// <summary>
        /// Parses the specified JSON <paramref name="element"/> into an instance of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of object to desrialize.</typeparam>
        /// <param name="element">The JSON element to read.</param>
        /// <returns>Deserialized representation of the JSON object.</returns>
        public static T ToObject<T>(this JsonElement element) => JsonSerializer.Deserialize<T>(element.GetSpan(), JsonSerialization.Options)!;

        /// <summary>
        /// Parses the specified JSON <paramref name="element"/> into an instance of the specified <paramref name="returnType"/>.
        /// </summary>
        /// <param name="element">The JSON element to read.</param>
        /// <param name="returnType">The type of object to desrialize.</param>
        /// <returns>Deserialized representation of the JSON object.</returns>
        public static object ToObject(this JsonElement element, Type returnType) => JsonSerializer.Deserialize(element.GetSpan(), returnType, JsonSerialization.Options)!;

        internal static JsonSerializerOptions ApplyOptions(this JsonSerializerOptions options)
        {
            options.DictionaryKeyPolicy = options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
            return options;
        }

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