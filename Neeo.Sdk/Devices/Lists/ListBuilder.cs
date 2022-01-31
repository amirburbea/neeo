using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Neeo.Sdk.Json;

namespace Neeo.Sdk.Devices.Lists;

public interface IListBuilder
{
    string? BrowseIdentifier { get; }

    IReadOnlyCollection<ListItemBase> Items { get; }

    [JsonPropertyName("_meta")]
    ListMetadata Metadata { get; }

    int Offset { get; }

    [JsonIgnore]
    ListParameters Parameters { get; }

    string Title { get; }

    int TotalMatchingItems { get; }

    IListBuilder AddButtonRow(ListButtonRow row);

    IListBuilder AddTileRow(ListTileRow row);

    IListBuilder AddEntry(ListEntry entry);

    IListBuilder AddHeader(ListHeader header);

    IListBuilder AddInfoItem(ListInfoItem infoItem);

    IListBuilder SetTitle(string? title = "");

    IListBuilder SetTotalMatchingItems(int totalMatchingItems);
}

internal sealed class ListBuilder : IListBuilder
{
    private readonly List<ListItemBase> _items = new();
    private readonly int _limit;

    public ListBuilder(ListParameters parameters)
    {
        (this.BrowseIdentifier, int limit, int? offset, string? title, int? totalMatchingItems) = this.Parameters = parameters;
        this.Offset = offset is int value and > 0 ? value : 0;
        this._limit = limit is > 0 and <= Constants.MaxItems ? limit : Constants.MaxItems;
        this.Title = Validator.ValidateText(title, minLength: 0, maxLength: 255, allowNull: true) ?? string.Empty;
        this.TotalMatchingItems = totalMatchingItems ?? 0;
        this.Metadata = this.BuildMetadata();
    }

    public string? BrowseIdentifier { get; }

    IReadOnlyCollection<ListItemBase> IListBuilder.Items => this._items;

    public ListMetadata Metadata { get; private set; }

    public int Offset { get; }

    public ListParameters Parameters { get; }

    public string Title { get; private set; }

    public int TotalMatchingItems { get; private set; }

    IListBuilder IListBuilder.AddButtonRow(ListButtonRow row) => this.AddItem(row ?? throw new ArgumentNullException(nameof(row)));

    IListBuilder IListBuilder.AddEntry(ListEntry entry) => this.AddItem(entry ?? throw new ArgumentNullException(nameof(entry)));

    IListBuilder IListBuilder.AddHeader(ListHeader header) => this.AddItem(header ?? throw new ArgumentNullException(nameof(header)));
    

    IListBuilder IListBuilder.AddTileRow(ListTileRow row) => this.AddItem(row ?? throw new ArgumentNullException(nameof(row)));



    IListBuilder IListBuilder.AddInfoItem(ListInfoItem infoItem) => this.AddItem(infoItem ?? throw new ArgumentNullException(nameof(infoItem)));

    IListBuilder IListBuilder.SetTitle(string? title) => this.SetTitle(title ?? string.Empty);

    IListBuilder IListBuilder.SetTotalMatchingItems(int totalMatchingItems) => this.SetTotalMatchingItems(totalMatchingItems);

    private ListBuilder AddItem(ListItemBase item)
    {
        this._items.Add(item);
        this.Build();
        return this;
    }

    private ListBuilder AddItems(IEnumerable<ListItemBase> items)
    {
        this._items.AddRange(items);
        this.Build();
        return this;
    }

    private void Build()
    {
        // Rebuild the metadata.
        this.Metadata = this.BuildMetadata();
        // Validate the list.
    }

    private ListMetadata BuildMetadata()
    {
        int entryCount = this._items.Count(item => item is ListEntry entry && !entry.UIAction.HasValue);
        int nextOffset = this.Offset + entryCount;
        return new(
            this,
            current: new(this, limit: this._limit, offset: this.Offset),
            previous: this.Offset > 0
                ? new(this, limit: Math.Min(this._limit, this.Offset), offset: Math.Max(this.Offset - this._limit, 0))
                : default,
            next: this.TotalMatchingItems > nextOffset && nextOffset != 0
                ? new(this, limit: this._limit, offset: nextOffset)
                : null
        );
    }

    private ListBuilder SetTitle(string title)
    {
        this.Title = Validator.ValidateText(title, minLength: 0, maxLength: 255);
        this.Build();
        return this;
    }

    private ListBuilder SetTotalMatchingItems(int totalMatchingItems = default)
    {
        this.TotalMatchingItems = Validator.ValidateNotNegative(totalMatchingItems);
        this.Build();
        return this;
    }
}