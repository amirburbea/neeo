namespace Remote.Utilities.TokenSearch
{
    public sealed class SearchItem<T>
        where T : notnull
    {
        public SearchItem(T item) => this.Item = item;

        public T Item { get; }

        public double Score { get; set; }

        internal object? GetValue(string propertyName) => TokenSearch<T>.GetItemValue(this.Item, propertyName);
    }
}