namespace Neeo.Sdk.Devices.Lists;

public sealed class ListPage
{
    internal ListPage(string? browseIdentifier, int limit, int offset)
    {
        this.BrowseIdentifier = browseIdentifier;
        this.Limit = limit;
        this.Offset = offset;
    }

    public string? BrowseIdentifier { get; }

    public int Limit { get; }

    public int Offset { get; }
}