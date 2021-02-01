using System;
using System.Reflection;

namespace Remote.Neeo
{
    internal static class AttributeValue
    {
        public static TData? GetEnumAttributeData<TAttribute, TData, TValue>(TValue value, Func<TAttribute, TData> projection)
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
