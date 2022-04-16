using System;
using System.Collections.Generic;
using System.Reflection;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// An attribute used to associate text with an enumeration value.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class TextAttribute : Attribute
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
    /// <typeparam name="TValue">The type of the enumerated value.</typeparam>
    /// <param name="text">The text from which to search for its related enumerated value.</param>
    /// <returns>Enumerated value if found, otherwise <see langword="null"/>.</returns>
    public static TValue? GetEnum<TValue>(string text)
        where TValue : struct, Enum => EnumMapping<TValue>.GetEnum(text);

    /// <summary>
    /// Gets the text associated with the specified enumerated value
    /// (falling back to <c>value.ToString()</c> if not found).
    /// </summary>
    /// <typeparam name="TValue">The type of the enumerated value.</typeparam>
    /// <param name="value">The enumerated value.</param>
    /// <returns>
    /// Text specified for the enumerated value via a <see cref="TextAttribute"/>,
    /// falling back to <c>value.ToString()</c> if not found.
    /// </returns>
    public static string GetText<TValue>(TValue value)
        where TValue : struct, Enum => EnumMapping<TValue>.GetText(value);

    private static class EnumMapping<T>
        where T : struct, Enum
    {
        private static readonly Dictionary<string, T> _fromText = new();
        private static readonly Dictionary<T, string> _toText = new();

        static EnumMapping()
        {
            T[] values = Enum.GetValues<T>();
            Dictionary<string, T> fromText = new(values.Length);
            Dictionary<T, string> toText = new(values.Length);
            foreach (T value in values)
            {
                if (Enum.GetName(value) is not { } name)
                {
                    continue;
                }
                string text = typeof(T).GetField(name, BindingFlags.Public | BindingFlags.Static)?.GetCustomAttribute<TextAttribute>()?.Text ?? name;
                fromText.Add(text, value);
                toText.Add(value, text);
            }
            (EnumMapping<T>._fromText, EnumMapping<T>._toText) = (fromText, toText);
        }

        public static T? GetEnum(string text) => EnumMapping<T>._fromText.TryGetValue(text, out T value) ? value : default(T?);

        public static string GetText(T value) => EnumMapping<T>._toText.TryGetValue(value, out string? text) ? text : value.ToString();
    }
}