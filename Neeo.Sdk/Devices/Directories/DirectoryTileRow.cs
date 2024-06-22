using System.Collections.Generic;

namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Defines a row of tiles (pictures) to be displayed in the directory.
/// </summary>
/// <param name="Tiles">The collection of tiles to display.</param>
internal sealed record DirectoryTileRow(
    IReadOnlyCollection<DirectoryTile> Tiles
) : IDirectoryItem
{
    DirectoryItemType IDirectoryItem.Type => DirectoryItemType.TileRow;
}