namespace Neeo.Sdk.Devices.Lists;

public sealed class ListPage
{
    private readonly IListBuilder _list;

    internal ListPage(IListBuilder list, int limit, int offset)
    {
        this._list = list;
        this.Limit = limit;
        this.Offset = offset;
    }

    public string? BrowseIdentifier => this._list.BrowseIdentifier;

    public int Limit { get; }

    public int Offset { get; }
}