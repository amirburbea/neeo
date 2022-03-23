namespace Neeo.Sdk.Devices.Lists;

public sealed class ListTile : ClickableListItem
{
    public ListTile(
        string thumbnailUri,
        string? actionIdentifier = default,
        ListUIAction? uiAction = default
    ) : base(actionIdentifier, uiAction)
    {
        this.ThumbnailUri = thumbnailUri;
    }

    /// <summary>
    /// Tells the NEEO Brain that this is a Tile.
    /// </summary>
    public bool IsTile { get; } = true;

    public string ThumbnailUri { get; }
}