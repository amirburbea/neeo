using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Remote.Neeo
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    internal sealed class TextAttribute : Attribute
    {
        public TextAttribute(string text) => this.Text = text;

        public string Text { get; }

        public static string GetEnumText<T>(T value)
            where T : struct, Enum
        {
            return EnumTextMapping<T>.ToText[value];
        }

        public sealed class EnumJsonConverter<T> : JsonConverter<T>
            where T : struct, Enum
        {
            public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return reader.GetString() is string text && EnumTextMapping<T>.FromText.TryGetValue(text, out T value) ? value : default;
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(EnumTextMapping<T>.ToText.TryGetValue(value, out string? text) ? text : value.ToString());
            }
        }

        private sealed class EnumTextMapping<T>
            where T : struct, Enum
        {
            public static readonly Dictionary<string, T> FromText = Enum.GetValues<T>().ToDictionary(
                value => AttributeData.GetEnumAttributeData(value, (TextAttribute attribute) => attribute.Text) ?? value.ToString()
            );

            public static readonly Dictionary<T, string> ToText = EnumTextMapping<T>.FromText.ToDictionary(
                pair => pair.Value, 
                pair => pair.Key
            );
        }
    }
}
