using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Lists;

public sealed class ListBuilder
{
    private readonly List<IListItem> _items = new();
    private readonly int _limit;

    internal ListBuilder(BrowseParameters parameters)
    {
        (this.BrowseIdentifier, int limit, int? offset) = this.Parameters = parameters;
        this.Offset = offset is int startIndex and > 0 ? startIndex : 0;
        this._limit = limit is > 0 and <= Constants.MaxItems ? limit : Constants.MaxItems;
        this.Build();
    }

    public string? BrowseIdentifier { get; }

    public IReadOnlyCollection<IListItem> Items => this._items;

    [JsonPropertyName("_meta")]
    public ListMetadata Metadata { get; private set; }

    public int Offset { get; }

    [JsonIgnore]
    public BrowseParameters Parameters { get; }

    public string Title { get; private set; } = string.Empty;

    public int TotalMatchingItems { get; private set; }

    public ListBuilder AddButtonRow(params ListButton[] buttons) => this.AddItem(new ListButtonRow(buttons ?? throw new ArgumentNullException(nameof(buttons))));

    public ListBuilder AddEntry(ListEntry entry) => this.AddItem(entry ?? throw new ArgumentNullException(nameof(entry)));

    public ListBuilder AddHeader(string title) => this.AddItem(new ListHeader(title ?? throw new ArgumentNullException(nameof(title))));

    public ListBuilder AddInfoItem(ListInfoItem infoItem) => this.AddItem(infoItem ?? throw new ArgumentNullException(nameof(infoItem)));

    public ListBuilder AddTileRow(params ListTile[] tiles) => this.AddItem(new ListTileRow(tiles ?? throw new ArgumentNullException(nameof(tiles))));

    public ListBuilder SetTitle(string title)
    {
        this.Title = title ?? string.Empty;
        return this.Build();
    }

    public ListBuilder SetTotalMatchingItems(int totalMatchingItems = default)
    {
        this.TotalMatchingItems = totalMatchingItems;
        return this.Build();
    }

    private ListBuilder AddItem(IListItem item)
    {
        this._items.Add(item);
        return this.Build();
    }

    private ListBuilder Build()
    {
        // Verify the list.
        Validator.ValidateText(this.Title, minLength: 0, maxLength: 255);
        Validator.ValidateNotNegative(this.TotalMatchingItems);
        this.Metadata = BuildMetadata();
        return this;

        ListMetadata BuildMetadata()
        {
            int entryCount = this._items.Count(item => item is ListEntry { UIAction: null });
            int nextOffset = this.Offset + entryCount;
            return new(
                this,
                current: new(this, offset: this.Offset),
                previous: this.Offset > 0
                    ? new(this, offset: Math.Max(this.Offset - this._limit, 0))
                    : default,
                next: this.TotalMatchingItems > nextOffset && nextOffset != 0
                    ? new(this, offset: nextOffset)
                    : default
            );
        }
    }
}