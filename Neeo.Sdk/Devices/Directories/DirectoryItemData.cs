using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Wire representation of a <see cref="DirectoryEntry"/>, produced by <see cref="DirectoryBuilder"/> after validation.
/// </summary>
internal sealed record class DirectoryEntryData(
    string Title,
    string? Label,
    string? BrowseIdentifier,
    string? ThumbnailUri,
    bool? IsQueueable,
    string? ActionIdentifier,
    DirectoryUIAction? UIAction
) : ClickableDirectoryItem(ActionIdentifier, UIAction), IDirectoryItem
{
    DirectoryItemType IDirectoryItem.Type => DirectoryItemType.Entry;
}

/// <summary>
/// Wire representation of a <see cref="DirectoryButton"/>, produced by <see cref="DirectoryBuilder"/> after validation.
/// </summary>
internal sealed record class DirectoryButtonData(
    string Text,
    [property: JsonPropertyName("iconName")] DirectoryButtonIcon? Icon,
    bool? Inverse,
    string? ActionIdentifier,
    DirectoryUIAction? UIAction
) : ClickableDirectoryItem(ActionIdentifier, UIAction)
{
    /// <summary>
    /// Tells the NEEO Brain that this is a Button.
    /// </summary>
    public bool IsButton { get; } = true;
}

/// <summary>
/// Wire representation of a <see cref="DirectoryTile"/>, produced by <see cref="DirectoryBuilder"/> after validation.
/// </summary>
internal sealed record class DirectoryTileData(
    string ThumbnailUri,
    string? ActionIdentifier,
    DirectoryUIAction? UIAction
) : ClickableDirectoryItem(ActionIdentifier, UIAction)
{
    /// <summary>
    /// Tells the NEEO Brain that this is a Tile.
    /// </summary>
    public bool IsTile { get; } = true;
}

/// <summary>
/// Wire representation of a <see cref="DirectoryInfoItem"/>, produced by <see cref="DirectoryBuilder"/> after validation.
/// </summary>
internal sealed record class DirectoryInfoItemData(
    string Title,
    string Text,
    string? ActionIdentifier,
    string? AffirmativeButtonText,
    string? NegativeButtonText
) : ClickableDirectoryItem(ActionIdentifier), IDirectoryItem
{
    /// <summary>
    /// Tells the NEEO Brain that this is an info item.
    /// </summary>
    public bool IsInfoItem { get; } = true;

    DirectoryItemType IDirectoryItem.Type => DirectoryItemType.InfoItem;
}
