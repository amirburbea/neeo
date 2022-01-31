namespace Neeo.Sdk.Devices.Lists;

public sealed class ListMetadata
{
    internal ListMetadata(int totalItems, int? totalMatchingItems, ListPage current, ListPage? previous, ListPage? next)
    {
        this.TotalItems = totalItems;
        this.TotalMatchingItems = totalMatchingItems;
        this.Current = current;
        this.Previous = previous;
        this.Next = next;
    }

    public ListPage Current { get; }

    public ListPage? Next { get; }

    public ListPage? Previous { get; }

    public int TotalItems { get; }

    public int? TotalMatchingItems { get; }
}