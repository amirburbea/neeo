namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// Basic information regarding a list page.
/// </summary>
public readonly struct ListPageInfo
{
    private readonly IListBuilder _list;

    internal ListPageInfo(IListBuilder list, int offset)
    {
        this._list = list;
        this.Offset = offset;
    }

    /// <summary>
    /// Gets the browse identifier.
    /// </summary>
    public string? BrowseIdentifier => this._list.BrowseIdentifier;

    /// <summary>
    /// Gets the maximum page size limit.
    /// </summary>
    public int Limit => this._list.Parameters.Limit;

    /// <summary>
    /// Gets the offset if this is not the first page (otherwise 0).
    /// </summary>
    public int Offset { get; }
}