using System;

namespace Neeo.Api.Utilities.TokenSearch;

public interface ISearchItem<T> 
    where T : notnull, IComparable<T>
{
    public T Item { get; }

    public double MaxScore { get; }

    public double Score { get; }

    
}

internal sealed record class SearchItem<T>(T Item) : ISearchItem<T>, IComparable<SearchItem<T>>
    where T : notnull, IComparable<T>
{
    public double MaxScore { get; set; }

    public double Score { get; set; }

    public object? GetValue(string propertyName) => TokenSearch<T>.GetItemValue(this.Item, propertyName);

    public int CompareTo(SearchItem<T>? other) => other is not null
        ? this.Score.CompareTo(other.Score) is int scoreComparison and not 0
            ? scoreComparison
            : this.Item.CompareTo(other.Item)
        : -1;
}