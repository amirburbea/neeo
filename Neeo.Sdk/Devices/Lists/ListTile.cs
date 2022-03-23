namespace Neeo.Sdk.Devices.Lists;

public sealed class ListTile : ClickableListItemBase
{
    /// <summary>
    /// Creates 
    /// </summary>
    /// <param name="thumbnailUri">The URI of the thumbnail to associate with the tile.</param>
    /// <param name="actionIdentifier">The (optional) action identifier.</param>
    /// <param name="uiAction">The (optional) action list UI action.</param>
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

    /// <summary>
    /// Gets the URI of the thumbnail to associate with the tile.
    /// </summary>
    public string ThumbnailUri { get; }
}