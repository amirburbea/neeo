namespace Neeo.Sdk.Devices.Lists;

public sealed class ListMetadata
{
    private readonly IListBuilder _list;

    internal ListMetadata(IListBuilder list, ListPage current, ListPage? previous, ListPage? next)
    {
        this._list = list;
        this.Current = current;
        this.Previous = previous;
        this.Next = next;
    }

    public ListPage Current { get; }

    public ListPage? Next { get; }

    public ListPage? Previous { get; }

    public int TotalItems => this._list.Items.Count;

    public int? TotalMatchingItems => this._list.TotalMatchingItems;
}