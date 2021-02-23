using System;
using System.Reflection;

namespace Remote.Neeo
{
    internal static class AttributeData
    {
        public static T? GetEnumAttributeData<TValue, TAttribute, T>(TValue value, Func<TAttribute, T> projection)
            where TValue : struct, Enum
            where TAttribute : Attribute
        {
            return Enum.GetName(value) is string name &&
                typeof(TValue).GetField(name, BindingFlags.Static | BindingFlags.Public) is FieldInfo field &&
                field.GetCustomAttribute<TAttribute>() is TAttribute attribute
                ? projection(attribute)
                : default;
        }
    }
}
