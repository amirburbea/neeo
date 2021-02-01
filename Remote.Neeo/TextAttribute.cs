using System;

namespace Remote.Neeo
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class TextAttribute : Attribute
    {
        public TextAttribute(string text) => this.Text = text;

        public string Text { get; }

        public static string GeEnumtText<TValue>(TValue value)
            where TValue : struct, Enum
        {
            return AttributeValue.GetEnumAttributeData(value, (TextAttribute attribute) => attribute.Text) ?? value.ToString();
        }
    }
}
