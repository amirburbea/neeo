using System;
using System.Collections.Generic;

namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// A row of <see cref="ListTile"/> images.
/// </summary>
public sealed class ListTileRow
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListTileRow"/> class.
    /// </summary>
    /// <param name="tiles">The collection of tiles in the row.</param>
    internal ListTileRow(ListTile[] tiles) => this.Tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));

    /// <summary>
    /// Gets the collection of tiles in the row.
    /// </summary>
    public IReadOnlyCollection<ListTile> Tiles { get; }
}