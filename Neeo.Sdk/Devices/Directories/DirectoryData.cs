using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Data associated with a directory.
/// </summary>
/// <param name="parameters"></param>
/// <param name="items">The items in the directory.</param>
/// <param name="entryCount">The number of items that are of type <c>Entry</c>.</param>
/// <param name="title">The title of the directory.</param>
/// <param name="totalMatchingItems">The total number of items across all pages.</param>
public readonly struct DirectoryData(
    BrowseParameters parameters,
    IReadOnlyCollection<IDirectoryItem> items,
    int entryCount,
    string title,
    int totalMatchingItems
)
{
    /// <summary>
    /// Gets the directory browse identifier.
    /// </summary>
    public string BrowseIdentifier => parameters.BrowseIdentifier;

    /// <summary>
    /// Gets the items in the directory.
    /// </summary>
    public IReadOnlyCollection<IDirectoryItem> Items => items;

    /// <summary>
    /// Gets the limit of the number of items to add in this specific page.
    /// </summary>
    public int Limit => parameters.Limit is > 0 and <= Constants.MaxItems and { } limit
        ? limit
        : Constants.MaxItems;

    /// <summary>
    /// Gets the metadata of the directory.
    /// </summary>
    [JsonPropertyName("_meta")]
    public DirectoryMetadata Metadata => new(
        items.Count,
        this.TotalMatchingItems,
        current: new(parameters, this.Offset),
        previous: this.Offset switch
        {
            int offset and not 0 => new(parameters, offset: Math.Max(offset - this.Limit, 0)),
            _ => null
        },
        next: (this.Offset + entryCount) switch
        {
            int nextOffset and not 0 when this.TotalMatchingItems > nextOffset => new(parameters, offset: nextOffset),
            _ => null,
        }
    );

    /// <summary>
    /// Gets the pagination offset.
    /// </summary>
    public int Offset => parameters.Offset is int startIndex and > 0
        ? startIndex
        : 0;

    /// <summary>
    /// Gets the title of the directory.
    /// </summary>
    public string Title => title;

    /// <summary>
    /// Gets the total number of items across all pages.
    /// </summary>
    public int TotalMatchingItems => Math.Max(entryCount, totalMatchingItems);
}
