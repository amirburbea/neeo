namespace Neeo.Sdk.Devices.Lists;

public sealed class ListEntry : ClickableListItem, IListItem
{
    public ListEntry(string title, string? label = default, string? browseIdentifier = default, string? actionIdentifier = default, string? thumbnailUri = default, bool? isQueueable = default, ListUIAction? uiAction = default)
        : base(actionIdentifier,  uiAction)
    {
        this.Title = title;
        this.Label = label;
        this.ThumbnailUri = thumbnailUri;
        this.IsQueueable = isQueueable;
        this.BrowseIdentifier = browseIdentifier;
    }

    public string? BrowseIdentifier { get; }

    public bool? IsQueueable { get; }

    public string? ThumbnailUri { get; }

    public string Title { get; }

    public string? Label { get; }

    ListItemType IListItem.Type => ListItemType.Entry;
}