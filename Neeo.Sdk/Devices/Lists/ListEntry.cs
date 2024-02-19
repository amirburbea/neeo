namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// An entry in a NEEO directory list.
/// </summary>
/// <remarks>
/// Initializes a new <see cref="ListEntry"/> instance.
/// </remarks>
/// <param name="title">The entry title.</param>
/// <param name="label">Optional label for the entry to use instead of the title.</param>
/// <param name="browseIdentifier">Optional - identifier of the directory to be browsed upon clicking this entry, typically used to point to a subdirectory.</param>
/// <param name="actionIdentifier">Optional - identifier of the action to be performed upon clicking this entry, typically used to point to a file to open.</param>
/// <param name="thumbnailUri">Optional - a URI of a thumbnail to display next to the entry in the list.</param>
/// <param name="uiAction">Optional - a special list UI action to be performed when this entry is clicked.</param>
public sealed class ListEntry(
    string title,
    string? label = default,
    string? browseIdentifier = default,
    string? actionIdentifier = default,
    string? thumbnailUri = default,
    ListUIAction? uiAction = default
    ) : ClickableListItemBase(actionIdentifier, uiAction)
{

    /// <summary>
    /// Gets the (optional) identifier of the directory to be browsed upon clicking this entry, typically used to point to a subdirectory.
    /// </summary>
    public string? BrowseIdentifier { get; } = browseIdentifier;

    /// <summary>
    /// Gets the (optional) URI of a thumbnail to display next to the entry in the list.
    /// </summary>
    public string? ThumbnailUri { get; } = thumbnailUri;

    /// <summary>
    /// Gets the entry title.
    /// </summary>
    public string Title { get; } = title;

    /// <summary>
    /// Gets the (optional) label for the entry to use instead of the title.
    /// </summary>
    public string? Label { get; } = label;
}