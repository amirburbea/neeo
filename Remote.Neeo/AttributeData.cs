using System;
using System.Reflection;

namespace Remote.Neeo
{
    internal static class AttributeData
    {
        public static TData? GetEnumAttributeData<TValue, TAttribute, TData>(TValue value, Func<TAttribute, TData> projection)
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
