using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Neeo.Sdk.Utilities;

internal sealed class CovariantReadOnlyDictionary<TKey, TValue, TBase> : IReadOnlyDictionary<TKey, TBase>
    where TValue : TBase
{
    private readonly IReadOnlyDictionary<TKey, TValue> _dictionary;

    public CovariantReadOnlyDictionary(IReadOnlyDictionary<TKey, TValue> dictionary) => this._dictionary = dictionary;

    public int Count => this._dictionary.Count;

    public IEnumerable<TKey> Keys => this._dictionary.Keys;

    public IEnumerable<TBase> Values => this._dictionary.Values.Cast<TBase>();

    public TBase this[TKey key] => this._dictionary[key];

    public bool ContainsKey(TKey key) => this._dictionary.ContainsKey(key);

    public IEnumerator<KeyValuePair<TKey, TBase>> GetEnumerator()
    {
        foreach (KeyValuePair<TKey, TValue> pair in this._dictionary)
        {
            yield return new KeyValuePair<TKey, TBase>(pair.Key, pair.Value);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TBase value)
    {
        if (this._dictionary.TryGetValue(key, out TValue? derived))
        {
            value = derived;
            return true;
        }
        value = default;
        return false;
    }
}