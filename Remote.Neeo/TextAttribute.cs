using System;

namespace Remote.Neeo
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    internal sealed class TextAttribute : Attribute
    {
        public TextAttribute(string text) => this.Text = text;

        public string Text { get; }

        public static string GeEnumText<TValue>(TValue value)
            where TValue : struct, Enum
        {
            return AttributeData.GetEnumAttributeData(value, (TextAttribute attribute) => attribute.Text) ?? value.ToString();
        }
    }
}
