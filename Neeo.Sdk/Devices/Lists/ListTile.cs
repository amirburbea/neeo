using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Lists;

public sealed class ListTile
{
    public ListTile(string thumbnailUri, string? actionIdentifier = default, bool? isQueuable = default, ListUIAction? uiAction = default)
    {
        this.ThumbnailUri = thumbnailUri;
        this.ActionIdentifier = actionIdentifier;
        this.IsQueuable = isQueuable ?? false;
        this.UIAction = uiAction;
    }

    public string? ActionIdentifier { get; }

    public bool IsQueuable { get; }

    /// <summary>
    /// Tells the NEEO Brain that this is a Tile.
    /// </summary>
    public bool IsTile { get; } = true;

    public string ThumbnailUri { get; }

    [JsonPropertyName("uiAction")]
    public ListUIAction? UIAction { get; }
}

public sealed class ListTileRow : ListItemBase
{
    internal ListTileRow(IEnumerable<ListTile> tiles)
        : base(ListItemType.TileRow) => this.Tiles = tiles.ToArray();

    public IReadOnlyCollection<ListTile> Tiles { get; }
}