using System.Collections.Generic;

namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Defines a row of buttons to be displayed in the directory.
/// </summary>
/// <param name="Buttons">The collection of buttons to display.</param>
internal sealed record class DirectoryButtonRow(
    IReadOnlyCollection<DirectoryButton> Buttons
) : IDirectoryItem
{
    DirectoryItemType IDirectoryItem.Type => DirectoryItemType.ButtonRow;
}