using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// Directory list builder.
/// </summary>
public sealed class ListBuilder
{
    private readonly List<object> _items = [];
    private readonly int _limit;

    internal ListBuilder(BrowseParameters parameters)
    {
        (this.BrowseIdentifier, int limit, int? offset) = this.Parameters = parameters;
        this._limit = limit is > 0 and < Constants.MaxItems ? limit : Constants.MaxItems;
        this.Offset = offset is int index and > 0 ? index : 0;
        this.Items = this._items.AsReadOnly();
        this.BuildMetadata();
    }

    /// <summary>
    /// Gets the dentifier of the directory being browsed.
    /// A value of blank or <see langword="null"/> often refers to the root.
    /// </summary>
    public string? BrowseIdentifier { get; }

    /// <summary>
    /// Gets the items in the list.
    /// </summary>
    public IReadOnlyCollection<object> Items { get; }

    /// <summary>
    /// Gets the list metadata.
    /// </summary>
    [JsonPropertyName("_meta")]
    public ListMetadata Metadata { get; private set; }

    /// <summary>
    /// Gets the pagination offset.
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// Gets the parameters used to initialize this <see cref="ListBuilder"/> instance.
    /// </summary>
    [JsonIgnore]
    public BrowseParameters Parameters { get; }

    /// <summary>
    /// Gets the title of the list.
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// In a paginated list, gets the total number of matching items (across all pages).
    /// </summary>
    public int TotalMatchingItems { get; private set; }

    /// <summary>
    /// Adds a row of buttons to the list.
    /// </summary>
    /// <param name="buttons">The buttons to add.</param>
    /// <returns><see cref="ListBuilder"/> for chaining.</returns>
    public ListBuilder AddButtonRow(params ListButton[] buttons) => this.AddItem(new ListButtonRow(buttons));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entry"></param>
    /// <returns><see cref="ListBuilder"/> for chaining.</returns>
    public ListBuilder AddEntry(ListEntry entry) => this.AddItem(entry);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="title"></param>
    /// <returns><see cref="ListBuilder"/> for chaining.</returns>
    public ListBuilder AddHeader(string title) => this.AddItem(new ListHeader(title));

    /// <summary>
    /// Adds an info item to the list which when clicked displays a dialog.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns><see cref="ListBuilder"/> for chaining.</returns>
    public ListBuilder AddInfoItem(ListInfoItem item) => this.AddItem(item);

    /// <summary>
    /// Adds a row of image tiles to the list.
    /// </summary>
    /// <param name="tiles">The image tiles to add.</param>
    /// <returns><see cref="ListBuilder"/> for chaining.</returns>
    public ListBuilder AddTileRow(params ListTile[] tiles) => this.AddItem(new ListTileRow(tiles));

    /// <summary>
    /// Sets the title of the list.
    /// </summary>
    /// <param name="title">The title of the list.</param>
    /// <returns><see cref="ListBuilder"/> for chaining.</returns>
    public ListBuilder SetTitle(string title)
    {
        this.Title = title ?? string.Empty;
        this.BuildMetadata();
        return this;
    }

    /// <summary>
    /// Sets the total number of matching items (across all pages of the list).
    /// </summary>
    /// <param name="totalMatchingItems">The total number of matching items.</param>
    /// <returns><see cref="ListBuilder"/> for chaining.</returns>
    public ListBuilder SetTotalMatchingItems(int totalMatchingItems)
    {
        this.TotalMatchingItems = totalMatchingItems;
        this.BuildMetadata();
        return this;
    }

    private ListBuilder AddItem(object item, [CallerArgumentExpression(nameof(item))] string argumentName = "")
    {
        ArgumentNullException.ThrowIfNull(item, argumentName);
        this._items.Add(item);
        this.BuildMetadata();
        return this;
    }

    private void BuildMetadata()
    {
        Validator.ValidateText(this.Title, minLength: 0, maxLength: 255);
        Validator.ValidateNotNegative(this.TotalMatchingItems);
        int entryCount = this._items.Count(item => item is ListEntry { UIAction: null });
        int nextOffset = this.Offset + entryCount;
        this.Metadata = new(
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