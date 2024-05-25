using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Neeo.Drivers.WebOS;

internal static class NameDictionary
{
    public static Dictionary<T, string> Generate<T, TAttribute>()
        where T : struct, Enum
        where TAttribute : Attribute, INameAttribute => new(
        from field in typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public)
        select KeyValuePair.Create(
            (T)field.GetValue(null)!,
            field.GetCustomAttribute<TAttribute>()?.Name ?? field.Name.ToLowerInvariant()
        )
    );
}

public interface INameAttribute
{
    string Name { get; }
}
