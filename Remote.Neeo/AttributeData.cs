using System;
using System.Reflection;

namespace Remote.Neeo;

internal static class AttributeData
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <typeparam name="TAttribute"></typeparam>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <param name="projection"></param>
    /// <returns></returns>
    public static T? GetEnumAttributeData<TValue, TAttribute, T>(TValue value, Func<TAttribute, T> projection)
        where TValue : struct, Enum
        where TAttribute : Attribute
    {
        return Enum.GetName(value) is { } name &&
            typeof(TValue).GetField(name, BindingFlags.Static | BindingFlags.Public) is { } field &&
            field.GetCustomAttribute<TAttribute>() is { } attribute
            ? projection(attribute)
            : default;
    }
}

