using System;

namespace Neeo.Sdk.Utilities.TokenSearch;

/// <summary>
/// Represents an entry in the search results.
/// </summary>
/// <typeparam name="T">The type of item being wrapped.</typeparam>
public sealed class SearchEntry<T>(T item) : IComparable<SearchEntry<T>>
    where T : notnull, IComparable<T>
{
    /// <summary>
    /// Gets the wrapped item.
    /// </summary>
    public T Item { get; } = item;

    /// <summary>
    /// Gets the maximum score of all entries in this result set.
    /// </summary>
    public double MaxScore { get; internal set; }

    /// <summary>
    /// Gets the score of the entry.
    /// </summary>
    public double Score { get; internal set; }

    /// <summary>
    /// Compares the current instance with another object of the same type and returns
    /// an integer that indicates whether the current instance precedes, follows, or
    /// occurs in the same position in the sort order as the other object.
    /// </summary>
    public int CompareTo(SearchEntry<T>? other)
    {
        if (other is null)
        {
            return -1;
        }
        return this.Score.CompareTo(other.Score) is int scoreComparison and not 0
            ? scoreComparison
            : this.Item.CompareTo(other.Item);
    }
}
