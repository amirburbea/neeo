namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Represents an entry - a file or directory - within a directory.
/// </summary>
/// <param name="Title">The title of the entry.</param>
/// <param name="Label">Optional - the label to use for the entry.</param>
/// <param name="BrowseIdentifier">The (optional) identifier of the directory to browse when clicked.</param>
/// <param name="ThumbnailUri">The (optional) thumbnail URI to associate with the entry.</param>
/// <param name="IsQueueable">Optional, specifies if the item can be added to the queue.</param>
/// <param name="ActionIdentifier">The (optional) action identifier.</param>
/// <param name="UIAction">The (optional) standardized directory UI action.</param>
public sealed record class DirectoryEntry(
    string Title,
    string? Label = null,
    string? BrowseIdentifier = null,
    string? ThumbnailUri = null,
    bool? IsQueueable = null,
    string? ActionIdentifier = null,
    DirectoryUIAction? UIAction = null
) : ClickableDirectoryItem(ActionIdentifier, UIAction), IDirectoryItem
{
    DirectoryItemType IDirectoryItem.Type => DirectoryItemType.Entry;
}
