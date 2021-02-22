using System;

namespace Remote.Utilities.TokenSearch
{
    public sealed class SearchItem<T> : IComparable<SearchItem<T>>
    {
        public SearchItem(T item) => this.Item = item;

        public T Item { get; }

        public double Score { get; set; }

        public int CompareTo(SearchItem<T>? other) => other == null ? 1 : this.Score.CompareTo(other.Score);

        public object? GetValue(string propertyName) => TokenSearch<T>.GetItemValue(this.Item, propertyName);
    }
}