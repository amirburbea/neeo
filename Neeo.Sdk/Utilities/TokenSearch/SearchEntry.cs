using System;

namespace Neeo.Sdk.Utilities.TokenSearch;

public sealed class SearchEntry<T> : IComparable<SearchEntry<T>>
    where T : notnull, IComparable<T>
{
    internal SearchEntry(T item) => this.Item = item;

    /// <summary>
    /// Gets the wrapped item.
    /// </summary>
    public T Item { get; }

    public double MaxScore { get; internal set; }

    public double Score { get; internal set; }

    /// <summary>
    /// Compares the current instance with another object of the same type and returns
    /// an integer that indicates whether the current instance precedes, follows, or
    /// occurs in the same position in the sort order as the other object.
    /// </summary>
    public int CompareTo(SearchEntry<T>? other) => other is not null
        ? this.Score.CompareTo(other.Score) is int scoreComparison and not 0
            ? scoreComparison
            : this.Item.CompareTo(other.Item)
        : -1;
}