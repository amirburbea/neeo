using System.Collections.Generic;
using System.Linq;

namespace Neeo.Sdk.Devices.Lists;

public sealed class ListTileRow : IDirectoryItem
{
    internal ListTileRow(IEnumerable<ListTile> tiles) => this.Tiles = tiles.ToArray();

    public IReadOnlyCollection<ListTile> Tiles { get; }

    DirectoryItemType IDirectoryItem.Type => DirectoryItemType.TileRow;
}
