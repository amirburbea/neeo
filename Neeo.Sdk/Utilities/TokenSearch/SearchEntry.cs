using System;

namespace Neeo.Sdk.Utilities.TokenSearch;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class SearchEntry<T> : IComparable<SearchEntry<T>>
    where T : notnull, IComparable<T>
{
    public SearchEntry(T item) => this.Item = item;

    public T Item { get; }

    public double MaxScore { get; internal set; }

    public double Score { get; internal set; }

    public int CompareTo(SearchEntry<T>? other) => other is not null
        ? this.Score.CompareTo(other.Score) is int scoreComparison and not 0
            ? scoreComparison
            : this.Item.CompareTo(other.Item)
        : -1;
}