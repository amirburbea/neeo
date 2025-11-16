namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Defines a (clickable) image tile.
/// </summary>
/// <param name="ThumbnailUri">The URI of the thumbnail to associate with the tile.</param>
/// <param name="ActionIdentifier">The (optional) action identifier.</param>
/// <param name="UIAction">The (optional) standardized directory UI action.</param>
public sealed record class DirectoryTile(
    string ThumbnailUri,
    string? ActionIdentifier = null,
    DirectoryUIAction? UIAction = null
) : ClickableDirectoryItem(ActionIdentifier, UIAction)
{
    /// <summary>
    /// Tells the NEEO Brain that this is a Tile.
    /// </summary>
    public bool IsTile { get; } = true;
}
