using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Neeo.Sdk.Json;

namespace Neeo.Sdk.Devices.Lists;

public interface IListBuilder
{
    IReadOnlyCollection<ListItemBase> Items { get; }

    int Limit { get; }

    [JsonPropertyName("_meta")]
    ListMetadata Metadata { get; }

    int Offset { get; }

    [JsonIgnore]
    ListParameters Parameters { get; }

    string Title { get; }

    IListBuilder AddButtonRow(ListButtonRow row);

    IListBuilder AddEntry(ListEntry entry);

    IListBuilder AddHeader(ListHeader header);

    IListBuilder AddInfoItem(ListInfoItem infoItem);

    IListBuilder SetTitle(string? title = "");

    IListBuilder SetTotalMatchingItems(int? totalMatchingItems = default);
}

internal sealed class ListBuilder : IListBuilder
{
    private readonly List<ListItemBase> _items = new();
    private int? _totalMatchingItems;

    public ListBuilder(ListParameters parameters)
    {
        (this.BrowseIdentifier, int limit, int? offset, string? title, this._totalMatchingItems) = this.Parameters = parameters;
        this.Offset = offset is int value and > 0 ? value : 0;
        this.Limit = limit is > 0 and <= Constants.MaxItems ? limit : Constants.MaxItems;
        this.Title = Validator.ValidateString(title, minLength: 0, maxLength: 255, allowNull: true) ?? string.Empty;
        this.Metadata = this.BuildMetadata();
    }

    public string? BrowseIdentifier { get; }

    IReadOnlyCollection<ListItemBase> IListBuilder.Items => this._items;

    public int Limit { get; }

    public ListMetadata Metadata { get; private set; }

    public int Offset { get; }

    public ListParameters Parameters { get; }

    public string Title { get; private set; }

    IListBuilder IListBuilder.AddButtonRow(ListButtonRow row) => this.AddItem(row ?? throw new ArgumentNullException(nameof(row)));

    IListBuilder IListBuilder.AddEntry(ListEntry entry) => this.AddItem(entry ?? throw new ArgumentNullException(nameof(entry)));

    IListBuilder IListBuilder.AddHeader(ListHeader header)
    {
        this.AddItem(header ?? throw new ArgumentNullException(nameof(header)));
        System.Diagnostics.Debug.WriteLine(System.Text.Json.JsonSerializer.Serialize(this.Metadata, JsonSerialization.Options));
        return this;
    }

    IListBuilder IListBuilder.AddInfoItem(ListInfoItem infoItem) => this.AddItem(infoItem ?? throw new ArgumentNullException(nameof(infoItem)));

    IListBuilder IListBuilder.SetTitle(string? title) => this.SetTitle(title ?? string.Empty);

    IListBuilder IListBuilder.SetTotalMatchingItems(int? totalMatchingItems) => this.SetTotalMatchingItems(totalMatchingItems);

    private ListBuilder SetTotalMatchingItems(int? totalMatchingItems = default)
    {
        this._totalMatchingItems = Validator.ValidateNotNegative(totalMatchingItems);
        this.Build();
        return this;
    }

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
        int totalItems = this._items.Count;
        int? totalMatchingItems = this._totalMatchingItems ;
        int entryCount = this._items.Count(item => item.Type == ListItemType.Entry);

        var previous =
            this.Offset > 0
                ? new(this.BrowseIdentifier, limit: Math.Min(this.Limit, this.Offset), offset: Math.Max(this.Offset - this.Limit, 0))
                : default(ListPage?);

        var next =
            totalMatchingItems.HasValue&& totalMatchingItems.Value > (this.Offset + this.Limit)
                ? new(this.BrowseIdentifier, limit: this.Limit, offset: (this.Offset + this.Limit))
                : default(ListPage?);

        return new(
            totalItems,
            totalMatchingItems??totalItems,
            current: new(this.BrowseIdentifier, limit: this.Limit, offset: this.Offset),
            previous,
            next
        );
    }

    private ListBuilder SetTitle(string title)
    {
        this.Title = Validator.ValidateString(title, minLength: 0, maxLength: 255);
        this.Build();
        return this;
    }
}