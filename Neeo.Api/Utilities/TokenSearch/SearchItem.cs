using System;

namespace Neeo.Api.Utilities.TokenSearch;

public interface ISearchItem<T> : IComparable<ISearchItem<T>>
    where T : notnull, IComparable<T>
{
    public T Item { get; }

    public double MaxScore { get; }

    public double Score { get; }

    int IComparable<ISearchItem<T>>.CompareTo(ISearchItem<T>? other)
    {
        if (other is null)
        {
            return -1;
        }
        if (this.Score != other.Score)
        {
            return this.Score.CompareTo(other.Score);
        }
        return this.Item.CompareTo(other.Item);
    }
}

public sealed class SearchItem<T> : ISearchItem<T>
    where T : notnull, IComparable<T>
{
    public SearchItem(T item) => this.Item = item;

    public T Item { get; }

    public double MaxScore { get; set; }

    public double Score { get; set; }

    internal object? GetValue(string propertyName) => TokenSearch<T>.GetItemValue(this.Item, propertyName);
}