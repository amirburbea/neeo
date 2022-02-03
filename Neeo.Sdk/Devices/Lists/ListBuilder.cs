using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Lists;

public interface IListBuilder
{
    string? BrowseIdentifier { get; }

    IReadOnlyCollection<IListItem> Items { get; }

    [JsonPropertyName("_meta")]
    ListMetadata Metadata { get; }

    int Offset { get; }

    [JsonIgnore]
    ListParameters Parameters { get; }

    string Title { get; }

    int TotalMatchingItems { get; }

    IListBuilder AddButtonRow(IEnumerable<ListButton> buttons) => this.AddButtonRow((buttons ?? throw new ArgumentNullException(nameof(buttons))).ToArray());

    IListBuilder AddButtonRow(params ListButton[] buttons);

    IListBuilder AddEntry(ListEntry entry);

    IListBuilder AddHeader(string title);

    IListBuilder AddInfoItem(ListInfoItem infoItem);

    IListBuilder AddTileRow(IEnumerable<ListTile> tiles) => this.AddTileRow((tiles ?? throw new ArgumentNullException(nameof(tiles))).ToArray());

    IListBuilder AddTileRow(params ListTile[] tiles);

    IListBuilder SetTitle(string? title = "");

    IListBuilder SetTotalMatchingItems(int totalMatchingItems);
}

internal sealed class ListBuilder : IListBuilder
{
    private readonly List<IListItem> _items = new();
    private readonly int _limit;

    public ListBuilder(ListParameters parameters)
    {
        (this.BrowseIdentifier, int limit, int? offset) = this.Parameters = parameters;
        this.Offset = offset is int value and > 0 ? value : 0;
        this._limit = limit is > 0 and <= Constants.MaxItems ? limit : Constants.MaxItems;
        this.Build();
    }

    public string? BrowseIdentifier { get; }

    IReadOnlyCollection<IListItem> IListBuilder.Items => this._items;

    public ListMetadata Metadata { get; private set; }

    public int Offset { get; }

    public ListParameters Parameters { get; }

    public string Title { get; private set; } = string.Empty;

    public int TotalMatchingItems { get; private set; }

    IListBuilder IListBuilder.AddButtonRow(ListButton[] buttons) => this.AddButtonRow(buttons);

    IListBuilder IListBuilder.AddEntry(ListEntry entry) => this.AddEntry(entry);

    IListBuilder IListBuilder.AddHeader(string title) => this.AddHeader(title);

    IListBuilder IListBuilder.AddInfoItem(ListInfoItem infoItem) => this.AddInfoItem(infoItem);

    IListBuilder IListBuilder.AddTileRow(ListTile[] tiles) => this.AddTileRow(tiles);

    IListBuilder IListBuilder.SetTitle(string? title) => this.SetTitle(title ?? string.Empty);

    IListBuilder IListBuilder.SetTotalMatchingItems(int totalMatchingItems) => this.SetTotalMatchingItems(totalMatchingItems);

    private ListBuilder AddButtonRow(ListButton[] buttons) => this.AddItem(new ListButtonRow(buttons ?? throw new ArgumentNullException(nameof(buttons))));

    private ListBuilder AddEntry(ListEntry entry) => this.AddItem(entry ?? throw new ArgumentNullException(nameof(entry)));

    private ListBuilder AddHeader(string title) => this.AddItem(new ListHeader(title ?? throw new ArgumentNullException(nameof(title))));

    private ListBuilder AddInfoItem(ListInfoItem infoItem) => this.AddItem(infoItem ?? throw new ArgumentNullException(nameof(infoItem)));

    private ListBuilder AddItem(IListItem item)
    {
        this._items.Add(item);
        return this.Build();
    }

    private ListBuilder AddTileRow(ListTile[] tiles) => this.AddItem(new ListTileRow(tiles ?? throw new ArgumentNullException(nameof(tiles))));

    private ListBuilder Build()
    {
        // Verify the list.
        Validator.ValidateText(this.Title, minLength: 0, maxLength: 255);
        Validator.ValidateNotNegative(this.TotalMatchingItems);
        //foreach (IListItem item in this._items)
        //{
        //    if (item is ClickableListItem clickable && (clickable.ActionIdentifier is null) == (clickable.BrowseIdentifier is null))
        //    {
        //        bool isNull = clickable.ActionIdentifier is null;
        //        throw new ValidationException($"A value {(isNull ? "should" : "can not")} be specified for {(isNull ? "either" : "both")} actionIdentifier {(isNull ? "or" : "and")} browseIdentifier.");
        //    }
        //}
        // Rebuild the metadata.
        this.Metadata = BuildMetadata();
        return this;

        ListMetadata BuildMetadata()
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
                    : default
            );
        }
    }

    private ListBuilder SetTitle(string title)
    {
        this.Title = title;
        return this.Build();
    }

    private ListBuilder SetTotalMatchingItems(int totalMatchingItems = default)
    {
        this.TotalMatchingItems = totalMatchingItems;
        return this.Build();
    }
}