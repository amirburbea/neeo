using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Remote.Neeo
{
    /// <summary>
    /// An attribute used to associate text with the decorated item.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    internal sealed class TextAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextAttribute"/> with the specified <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The text associated with the decorated item.</param>
        public TextAttribute(string text) => this.Text = text;

        /// <summary>
        /// The text associated with the decorated item.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Attempts to find the enumerated value associated with the specified text.
        /// </summary>
        /// <typeparam name="T">The type of the enumerated value.</typeparam>
        /// <param name="text">The text from which to search for its related enumerated value.</param>
        /// <returns>Enumerated value if found, otherwise <c>null</c>.</returns>
        public static T? GetEnum<T>(string text)
            where T : struct, Enum
        {
            return EnumMapping<T>.GetEnum(text);
        }

        /// <summary>
        /// Gets the text associated with the specified enumerated value (falling back to <c>value.ToString()</c> if not found).
        /// </summary>
        /// <typeparam name="T">The type of the enumerated value.</typeparam>
        /// <param name="value">The enumerated value.</param>
        /// <returns>Text specified for the enumerated value via a <see cref="TextAttribute"/>, falling back to <c>value.ToString()</c> if not found.</returns>
        public static string GetEnumText<T>(T value)
            where T : struct, Enum
        {
            return EnumMapping<T>.GetEnumText(value);
        }

        /// <summary>
        /// Supports serialization/deserialization of enumerated values to/from their respective text.
        /// </summary>
        /// <typeparam name="T">The type of the enumerated value.</typeparam>
        public sealed class EnumJsonConverter<T> : JsonConverter<T>
            where T : struct, Enum
        {
            public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return reader.GetString() is string text && EnumMapping<T>.GetEnum(text) is T value ? value : default;
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(EnumMapping<T>.GetEnumText(value));
            }
        }

        private sealed class EnumMapping<T>
            where T : struct, Enum
        {
            private static readonly Dictionary<string, T> _fromText = Enum.GetValues<T>().ToDictionary(
                value => AttributeData.GetEnumAttributeData(value, (TextAttribute attribute) => attribute.Text) ?? value.ToString()
            );

            private static readonly Dictionary<T, string> _toText = EnumMapping<T>._fromText.ToDictionary(
                pair => pair.Value,
                pair => pair.Key
            );

            public static T? GetEnum(string text) => EnumMapping<T>._fromText.TryGetValue(text, out T value) ? value : default(T?);

            public static string GetEnumText(T value) => EnumMapping<T>._toText.TryGetValue(value, out string? text) ? text : value.ToString();
        }
    }
}
