namespace Neeo.Sdk.Devices.Lists;

public sealed class ListEntry(string title, string? label = default, string? browseIdentifier = default, string? actionIdentifier = default, string? thumbnailUri = default, bool? isQueueable = default, ListUIAction? uiAction = default) : ClickableListItemBase(actionIdentifier, uiAction), IListItem
{
    public string? BrowseIdentifier { get; } = browseIdentifier;

    public bool? IsQueueable { get; } = isQueueable;

    public string? Label { get; } = label;
    public string? ThumbnailUri { get; } = thumbnailUri;

    public string Title { get; } = title;
    ListItemType IListItem.Type => ListItemType.Entry;
}
