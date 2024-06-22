using System.ComponentModel;

namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Represents a directory header row.
/// </summary>
/// <param name="Title">The title of the header.</param>
internal sealed record class DirectoryHeader(string Title) : IDirectoryItem
{
    /// <summary>
    /// Tells the NEEO Brain that this is a Header.
    /// </summary>
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public bool IsHeader { get; } = true;

    DirectoryItemType IDirectoryItem.Type => DirectoryItemType.Header;
}
