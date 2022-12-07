using System;
using System.Collections.Generic;

namespace Neeo.Sdk.Utilities.TokenSearch;

public sealed class SearchEntry<T>
    where T : notnull, IComparable<T>
{
    internal SearchEntry(T item) => this.Item = item;

    /// <summary>
    /// Gets the wrapped item.
    /// </summary>
    public T Item { get; }

    public double MaxScore { get; internal set; }

    public double Score { get; internal set; }
}

internal sealed class SearchEntryComparer<T> : Comparer<SearchEntry<T>>
    where T : notnull, IComparable<T>
{
    public new static readonly SearchEntryComparer<T> Default = new();

    public override int Compare(SearchEntry<T>? x, SearchEntry<T>? y)
    {
        return x!.Score.CompareTo(y!.Score) is int scoreComparison and not 0
            ? scoreComparison
            : x.Item.CompareTo(y.Item);
    }
}