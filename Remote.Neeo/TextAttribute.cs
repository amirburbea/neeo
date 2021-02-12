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

        public static T? GetEnum<T>(string text)
            where T : struct, Enum => EnumTextMapping<T>.GetEnum(text);

        public static string GetEnumText<T>(T value)
            where T : struct, Enum => EnumTextMapping<T>.GetEnumText(value);

        public sealed class EnumJsonConverter<T> : JsonConverter<T>
            where T : struct, Enum
        {
            public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return reader.GetString() is string text && EnumTextMapping<T>.GetEnum(text) is T value ? value : default;
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => writer.WriteStringValue(EnumTextMapping<T>.GetEnumText(value));
        }

        private sealed class EnumTextMapping<T>
            where T : struct, Enum
        {
            private static readonly Dictionary<string, T> _fromText = Enum.GetValues<T>().ToDictionary(value => AttributeData.GetEnumAttributeData(value, (TextAttribute attribute) => attribute.Text) ?? value.ToString());

            private static readonly Dictionary<T, string> _toText = EnumTextMapping<T>._fromText.ToDictionary(pair => pair.Value, pair => pair.Key);

            public static T? GetEnum(string text) => EnumTextMapping<T>._fromText.TryGetValue(text, out T value) ? value : default(T?);

            public static string GetEnumText(T value) => EnumTextMapping<T>._toText.TryGetValue(value, out string? text) ? text : value.ToString();
        }
    }
}
