namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// Metadata relating to an <see cref="ListBuilder"/>.
/// </summary>
public readonly struct ListMetadata
{
    private readonly ListBuilder _list;

    internal ListMetadata(ListBuilder list, ListPageInfo current, ListPageInfo? previous, ListPageInfo? next)
    {
        (this._list, this.Current, this.Previous, this.Next) = (list, current, previous, next);
    }

    /// <summary>
    /// Gets information about the current page of data.
    /// </summary>
    public ListPageInfo Current { get; }

    /// <summary>
    /// In a paginated list, gets information about the next page of data if it exists (otherwise <see langword="null"/>).
    /// </summary>
    public ListPageInfo? Next { get; }

    /// <summary>
    /// In a paginated list, gets information about the previous page of data if it exists (otherwise <see langword="null"/>).
    /// </summary>
    public ListPageInfo? Previous { get; }

    /// <summary>
    /// Gets the total number of items in the list.
    /// </summary>
    public int TotalItems => this._list.Items.Count;

    /// <summary>
    /// Gets the total matching item count for the list query (including data not on the current page).
    /// </summary>
    public int? TotalMatchingItems => this._list.TotalMatchingItems;
}