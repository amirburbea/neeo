namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// Represents a list header row.
/// </summary>
public sealed class ListHeader : IDirectoryItem
{
    internal ListHeader(string title) => this.Title = Validator.ValidateText(title, maxLength: 255);

    /// <summary>
    /// Tells the NEEO Brain that this is a Header.
    /// </summary>
    public bool IsHeader { get; } = true;

    /// <summary>
    /// The title for the list header.
    /// </summary>
    public string Title { get; }

    DirectoryItemType IDirectoryItem.Type => DirectoryItemType.Header;
}
