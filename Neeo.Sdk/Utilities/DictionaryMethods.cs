using System;
using System.Collections.Generic;

namespace Neeo.Sdk.Utilities;

public static class DictionaryMethods
{
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> factory) 
        where TKey : notnull
    {
        if (dictionary.TryGetValue(key, out TValue? value))
        {
            return value;
        }
        dictionary.Add(key, value = factory(key));
        return value;
    }

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
        where TValue : new()
    {
        return dictionary.GetOrAdd(key, _ => new());
    }
}
