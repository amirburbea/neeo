using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Builder used to populate NEEO directories.
/// </summary>
public sealed class DirectoryBuilder
{
    private readonly List<IDirectoryItem> _items = [];

    internal DirectoryBuilder(BrowseParameters parameters)
    {
        this.Parameters = parameters;
        this.SetMetadata();
    }

    /// <summary>
    /// Gets the identifier of the directory being browsed.
    /// </summary>
    public string? BrowseIdentifier => this.Parameters.BrowseIdentifier;

    /// <summary>
    /// Gets a value indicating if the current directory page is not full.
    /// </summary>
    [JsonIgnore]
    public bool CanAddEntry => this.Items.Count == 0 || this.Items.Count(item => item.Type == DirectoryItemType.Entry) < this.Limit;

    /// <summary>
    /// Gets the items within the directory.
    /// </summary>
    public IReadOnlyCollection<IDirectoryItem> Items => this._items;

    /// <summary>
    /// For pagination, gets the upper limit for number of entries to return in a single page.
    /// </summary>
    public int Limit => this.Parameters.Limit is int limit and > 0 and <= Constants.MaxItems
        ? limit
        : Constants.MaxItems;

    /// <summary>
    /// Gets a set of metadata relating to the current directory.
    /// </summary>
    [JsonPropertyName("_meta")]
    public DirectoryMetadata Metadata { get; private set; }

    /// <summary>
    /// Gets the offset from 0 (used in pagination).
    /// </summary>
    public int Offset => this.Parameters.Offset is int startIndex and > 0
        ? startIndex
        : 0;

    /// <summary>
    /// The parameters used to construct this instance.
    /// </summary>
    [JsonIgnore]
    public BrowseParameters Parameters { get; }

    /// <summary>
    /// Gets the title of the current directory.
    /// </summary>
    /// <remarks>Set via <see cref="SetTitle"/>.</remarks>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the total number of matching items (as oppposed to what only fits on the current page).
    /// </summary>
    /// <remarks>Set via <see cref="SetTotalMatchingItems"/>.</remarks>
    public int TotalMatchingItems { get; private set; }

    /// <summary>
    /// Adds a row of buttons to the directory.
    /// </summary>
    /// <param name="buttons">The array of buttons to add.</param>
    /// <returns><see cref="DirectoryBuilder"/> instance for chaining.</returns>
    public DirectoryBuilder AddButtonRow(params DirectoryButton[] buttons)
    {
        if (buttons is not { Length: > 0 and <= Constants.MaxButtonsPerRow })
        {
            throw new ArgumentException($"Array must not be null or empty and have length <= {Constants.MaxButtonsPerRow}.", nameof(buttons));
        }
        return this.AddItem(new DirectoryButtonRow(buttons));
    }

    /// <summary>
    /// Adds an entry - which represents a file or directory - to the directory.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <returns><see cref="DirectoryBuilder"/> instance for chaining.</returns>
    public DirectoryBuilder AddEntry(DirectoryEntry entry)
    {
        return this.AddItem(entry ?? throw new ArgumentNullException(nameof(entry)));
    }

    /// <summary>
    /// Adds a header with the specified title to the directory.
    /// </summary>
    /// <param name="title">The title of the header.</param>
    /// <returns><see cref="DirectoryBuilder"/> instance for chaining.</returns>
    public DirectoryBuilder AddHeader(string title)
    {
        return this.AddItem(new DirectoryHeader(Validator.ValidateText(title, maxLength: 255)));
    }

    /// <summary>
    /// Adds an info item dialog to the directory.
    /// </summary>
    /// <param name="triggerText">The text of the button to trigger the dialog.</param>
    /// <param name="dialogText">The text of the info dialog.</param>
    /// <param name="actionIdentifier">Optional action identifier for the button.</param>
    /// <param name="affirmativeButtonText">Text for the "OK" button.</param>
    /// <param name="negativeButtonText">Text for the Cancel/Close button.</param>
    /// <returns><see cref="DirectoryBuilder"/> instance for chaining.</returns>
    public DirectoryBuilder AddInfoItem(
        string triggerText,
        string dialogText,
        string? actionIdentifier = null,
        string? affirmativeButtonText = null,
        string? negativeButtonText = null
    ) => this.AddItem(new DirectoryInfoItem(triggerText, dialogText, actionIdentifier, affirmativeButtonText, negativeButtonText));

    /// <summary>
    /// Adds a row of tiles (pictures) to the directory.
    /// </summary>
    /// <param name="tiles">The array of tiles to add.</param>
    /// <returns><see cref="DirectoryBuilder"/> instance for chaining.</returns>
    public DirectoryBuilder AddTileRow(params DirectoryTile[] tiles)
    {
        if (tiles is not { Length: > 0 and <= Constants.MaxTilesPerRow })
        {
            throw new ArgumentException($"Array must not be null or empty and have length <= {Constants.MaxTilesPerRow}.", nameof(tiles));
        }
        return this.AddItem(new DirectoryTileRow(tiles));
    }

    /// <summary>
    /// Sets the title of the directory.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <returns><see cref="DirectoryBuilder"/> instance for chaining.</returns>
    public DirectoryBuilder SetTitle(string title)
    {
        this.Title = Validator.ValidateText(title ?? string.Empty, minLength: 0, maxLength: 255);
        this.SetMetadata();
        return this;
    }

    /// <summary>
    /// In pagination, sets the total number of matching items (as oppposed to what only fits on the current page).
    /// </summary>
    /// <param name="totalMatchingItems">The total number of matching items.</param>
    /// <returns><see cref="DirectoryBuilder"/> instance for chaining.</returns>
    public DirectoryBuilder SetTotalMatchingItems(int totalMatchingItems = default)
    {
        this.TotalMatchingItems = Validator.ValidateNotNegative(totalMatchingItems);
        this.SetMetadata();
        return this;
    }

    private DirectoryBuilder AddItem(IDirectoryItem item)
    {
        this._items.Add(item);
        this.SetMetadata();
        return this;
    }

    private void SetMetadata()
    {
        int entryCount = this.Items.Count(item => item.Type == DirectoryItemType.Entry);
        if (entryCount > this.TotalMatchingItems)
        {
            this.TotalMatchingItems = entryCount;
        }
        int nextOffset = this.Offset + entryCount;
        this.Metadata = new(
            this,
            current: new(this, this.Offset),
            previous: this.Offset == 0
                ? null
                : new(this, offset: Math.Max(this.Offset - this.Limit, 0)),
            next: nextOffset == 0 || this.TotalMatchingItems <= nextOffset
                ? null
                : new(this, offset: nextOffset)
        );
    }
}
