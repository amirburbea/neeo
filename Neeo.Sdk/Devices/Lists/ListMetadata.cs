namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// Metadata relating to an <see cref="IListBuilder"/>.
/// </summary>
public readonly struct ListMetadata
{
    private readonly IListBuilder _list;

    internal ListMetadata(IListBuilder list, ListPage current, ListPage? previous, ListPage? next)
    {
        (this._list, this.Current, this.Previous, this.Next) = (list, current, previous, next);
    }

    /// <summary>
    /// Gets information about the current page of data.
    /// </summary>
    public ListPage Current { get; }

    /// <summary>
    /// In a paginated list, gets information about the next page of data.
    /// </summary>
    public ListPage? Next { get; }

    /// <summary>
    /// In a paginated list not on the first page, gets information about the previous page of data.
    /// </summary>
    public ListPage? Previous { get; }

    /// <summary>
    /// Gets the total number of items in the list.
    /// </summary>
    public int TotalItems => this._list.Items.Count;

    /// <summary>
    /// Gets the total matching item count for the list query (including data not on the current page).
    /// </summary>
    public int? TotalMatchingItems => this._list.TotalMatchingItems;
}