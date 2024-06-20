namespace Neeo.Sdk.Devices.Lists;

public sealed class DirectoryEntry(
    string title,
    string? label = default,
    string? browseIdentifier = default,
    string? thumbnailUri = default,
    bool? isQueueable = default,
    string? actionIdentifier = default,
    DirectoryUIAction? uiAction = default
) : ClickableDirectoryItemBase(actionIdentifier, uiAction), IDirectoryItem
{
    public string? BrowseIdentifier => browseIdentifier;

    public bool? IsQueueable => isQueueable;

    public string? Label => label;

    public string? ThumbnailUri => thumbnailUri;

    public string Title => title;

    DirectoryItemType IDirectoryItem.Type => DirectoryItemType.Entry;
}
