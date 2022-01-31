using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Lists;

public sealed class ListEntry : ListItemBase
{
    public ListEntry(string title, string? browseIdentifier = default, string? actionIdentifier = default, string? thumbnailUri = default, bool? isQueueable = default, ListUIAction? uiAction = default)
        : base(ListItemType.Entry)
    {
        this.Title = title;
        this.BrowseIdentifier = browseIdentifier;
        this.ActionIdentifier = actionIdentifier;
        this.UIAction = uiAction;
        this.ThumbnailUri = thumbnailUri;
        this.IsQueueable = isQueueable??false;
    }

    public string? ActionIdentifier { get; }

    public string? BrowseIdentifier { get; }

    public string? ThumbnailUri { get; }
    public bool IsQueueable { get; }
    public string Title { get; }

    [JsonPropertyName("uiAction")]
    public ListUIAction? UIAction { get; }
}