using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// Builder used to populate NEEO directories.
/// </summary>
public sealed class DirectoryBuilder
{
    private readonly List<IDirectoryItem> _items = [];
    private readonly int _limit;

    internal DirectoryBuilder(BrowseParameters parameters)
    {
        (this.BrowseIdentifier, int limit, int? offset) = this.Parameters = parameters;
        this.Offset = offset is int startIndex and > 0 ? startIndex : 0;
        this._limit = limit is > 0 and <= Constants.MaxItems ? limit : Constants.MaxItems;
        this.Build();
    }

    public string? BrowseIdentifier { get; }

    /// <summary>
    /// Gets the items within the directory.
    /// </summary>
    public IReadOnlyCollection<IDirectoryItem> Items => this._items;

    [JsonPropertyName("_meta")]
    public DirectoryMetadata Metadata { get; private set; }

    /// <summary>
    /// Gets the offset from 0 (used in pagination).
    /// </summary>
    public int Offset { get; }

    [JsonIgnore]
    public BrowseParameters Parameters { get; }

    public string Title { get; private set; } = string.Empty;

    public int TotalMatchingItems { get; private set; }

    public DirectoryBuilder AddButtonRow(params DirectoryButton[] buttons) => this.AddItem(new DirectoryButtonRow(buttons ?? throw new ArgumentNullException(nameof(buttons))));

    public DirectoryBuilder AddEntry(DirectoryEntry entry) => this.AddItem(entry ?? throw new ArgumentNullException(nameof(entry)));

    public DirectoryBuilder AddHeader(string title) => this.AddItem(new ListHeader(title ?? throw new ArgumentNullException(nameof(title))));

    public DirectoryBuilder AddInfoItem(DirectoryInfoItem infoItem) => this.AddItem(infoItem ?? throw new ArgumentNullException(nameof(infoItem)));

    public DirectoryBuilder AddTileRow(params ListTile[] tiles) => this.AddItem(new ListTileRow(tiles ?? throw new ArgumentNullException(nameof(tiles))));

    public DirectoryBuilder SetTitle(string title)
    {
        this.Title = title ?? string.Empty;
        return this.Build();
    }

    public DirectoryBuilder SetTotalMatchingItems(int totalMatchingItems = default)
    {
        this.TotalMatchingItems = totalMatchingItems;
        return this.Build();
    }

    private DirectoryBuilder AddItem(IDirectoryItem item)
    {
        this._items.Add(item);
        return this.Build();
    }

    private DirectoryBuilder Build()
    {
        // Verify the list.
        Validator.ValidateText(this.Title, minLength: 0, maxLength: 255);
        Validator.ValidateNotNegative(this.TotalMatchingItems);
        this.Metadata = BuildMetadata();
        return this;

        DirectoryMetadata BuildMetadata()
        {
            int entryCount = this._items.Count(item => item is DirectoryEntry { UIAction: null });
            int nextOffset = this.Offset + entryCount;
            return new(
                this,
                current: new(this, offset: this.Offset),
                previous: this.Offset == 0
                    ? null
                    : new(this, offset: Math.Max(this.Offset - this._limit, 0)),
                next: this.TotalMatchingItems <= nextOffset || nextOffset == 0
                    ? null
                    : new(this, offset: nextOffset)
            );
        }
    }
}
