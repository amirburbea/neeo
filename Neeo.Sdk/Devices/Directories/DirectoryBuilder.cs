using System;
using System.Collections.Generic;

namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Builder used to populate NEEO directories.
/// </summary>
public interface IDirectoryBuilder
{
    /// <summary>
    /// Gets the identifier of the directory being browsed.
    /// </summary>
    string BrowseIdentifier { get; }

    /// <summary>
    /// Gets a value indicating if the current directory page is not full.
    /// </summary>
    bool CanAddEntry => this.EntryCount < this.Parameters.Limit;

    /// <summary>
    /// Gets the number of entries in the directory.
    /// </summary>
    int EntryCount { get; }

    /// <summary>
    /// Gets the items within the directory.
    /// </summary>
    IReadOnlyCollection<IDirectoryItem> Items { get; }

    /// <summary>
    /// The parameters used to construct this directory.
    /// </summary>
    BrowseParameters Parameters { get; }

    /// <summary>
    /// Gets the title of the current directory.
    /// </summary>
    /// <remarks>Set via <see cref="SetTitle"/>.</remarks>
    string Title { get; }

    /// <summary>
    /// Gets the total number of matching items across all pages.
    /// </summary>
    /// <remarks>Set via <see cref="SetTotalMatchingItems"/>.</remarks>
    int TotalMatchingItems { get; }

    /// <summary>
    /// Adds a row of buttons to the directory.
    /// </summary>
    /// <param name="buttons">The array of buttons to add.</param>
    /// <returns><see cref="IDirectoryBuilder"/> instance for chaining.</returns>
    IDirectoryBuilder AddButtonRow(params DirectoryButton[] buttons);

    /// <summary>
    /// Adds an entry - which represents a file or directory - to the directory.
    /// </summary>
    /// <param name="entry">The entry to add.</param>
    /// <returns><see cref="IDirectoryBuilder"/> instance for chaining.</returns>
    IDirectoryBuilder AddEntry(DirectoryEntry entry);

    /// <summary>
    /// Adds a header with the specified title to the directory.
    /// </summary>
    /// <param name="title">The title of the header.</param>
    /// <returns><see cref="IDirectoryBuilder"/> instance for chaining.</returns>
    IDirectoryBuilder AddHeader(string title);

    /// <summary>
    /// Adds an info item to the directory.
    /// </summary>
    /// <param name="infoItem">The info item to add.</param>
    /// <returns><see cref="IDirectoryBuilder"/> instance for chaining.</returns>
    IDirectoryBuilder AddInfoItem(DirectoryInfoItem infoItem);

    /// <summary>
    /// Adds a row of tiles (pictures) to the directory.
    /// </summary>
    /// <param name="tiles">The array of tiles to add.</param>
    /// <returns><see cref="IDirectoryBuilder"/> instance for chaining.</returns>
    IDirectoryBuilder AddTileRow(params DirectoryTile[] tiles);

    /// <summary>
    /// Sets the title of the directory.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <returns><see cref="IDirectoryBuilder"/> instance for chaining.</returns>
    IDirectoryBuilder SetTitle(string title);

    /// <summary>
    /// In pagination, sets the total number of matching items across all pages.
    /// </summary>
    /// <param name="totalMatchingItems">The total number of matching items.</param>
    /// <returns><see cref="IDirectoryBuilder"/> instance for chaining.</returns>
    IDirectoryBuilder SetTotalMatchingItems(int totalMatchingItems = 0);
}

internal sealed class DirectoryBuilder(BrowseParameters parameters) : IDirectoryBuilder
{
    private readonly List<IDirectoryItem> _items = [];

    public int EntryCount { get; private set; }
    public IReadOnlyCollection<IDirectoryItem> Items => this._items;
    public BrowseParameters Parameters => parameters;
    public string Title { get; private set; } = string.Empty;
    public int TotalMatchingItems { get; private set; }
    public string BrowseIdentifier => parameters.BrowseIdentifier;

    public DirectoryBuilder AddButtonRow(params DirectoryButton[] buttons)
    {
        if (buttons is not { Length: > 0 and <= Constants.MaxButtonsPerRow })
        {
            throw new ArgumentException($"Array must not be null or empty and have length <= {Constants.MaxButtonsPerRow}.", nameof(buttons));
        }
        DirectoryButtonData[] data = new DirectoryButtonData[buttons.Length];
        for (int index = 0; index < buttons.Length; index++)
        {
            data[index] = DirectoryBuilder.ToData(buttons[index] ?? throw new ArgumentException("Button must not be null.", nameof(buttons)));
        }
        return this.AddItem(new DirectoryButtonRow(data));
    }

    public DirectoryBuilder AddEntry(DirectoryEntry entry)
    {
        return this.AddItem(DirectoryBuilder.ToData(entry ?? throw new ArgumentNullException(nameof(entry))));
    }

    public DirectoryBuilder AddHeader(string title)
    {
        return this.AddItem(new DirectoryHeader(Validator.ValidateText(title, maxLength: 255)));
    }

    public DirectoryBuilder AddInfoItem(DirectoryInfoItem infoItem)
    {
        return this.AddItem(DirectoryBuilder.ToData(infoItem ?? throw new ArgumentNullException(nameof(infoItem))));
    }

    public DirectoryBuilder AddTileRow(params DirectoryTile[] tiles)
    {
        if (tiles is not { Length: > 0 and <= Constants.MaxTilesPerRow })
        {
            throw new ArgumentException($"Array must not be null or empty and have length <= {Constants.MaxTilesPerRow}.", nameof(tiles));
        }
        DirectoryTileData[] data = new DirectoryTileData[tiles.Length];
        for (int index = 0; index < tiles.Length; index++)
        {
            data[index] = DirectoryBuilder.ToData(tiles[index] ?? throw new ArgumentException("Tile must not be null.", nameof(tiles)));
        }
        return this.AddItem(new DirectoryTileRow(data));
    }

    public DirectoryData Build() => new(
        parameters,
        this._items,
        this.EntryCount,
        this.Title,
        this.TotalMatchingItems
    );

    public DirectoryBuilder SetTitle(string title)
    {
        this.Title = Validator.ValidateText(title ?? string.Empty, minLength: 0, maxLength: 255);
        return this;
    }

    public DirectoryBuilder SetTotalMatchingItems(int totalMatchingItems = default)
    {
        this.TotalMatchingItems = Validator.ValidateNotNegative(totalMatchingItems);
        return this;
    }

    IDirectoryBuilder IDirectoryBuilder.AddButtonRow(params DirectoryButton[] buttons) => this.AddButtonRow(buttons);
    IDirectoryBuilder IDirectoryBuilder.AddEntry(DirectoryEntry entry) => this.AddEntry(entry);
    IDirectoryBuilder IDirectoryBuilder.AddHeader(string title) => this.AddHeader(title);
    IDirectoryBuilder IDirectoryBuilder.AddInfoItem(DirectoryInfoItem infoItem) => this.AddInfoItem(infoItem);
    IDirectoryBuilder IDirectoryBuilder.AddTileRow(params DirectoryTile[] tiles) => this.AddTileRow(tiles);
    IDirectoryBuilder IDirectoryBuilder.SetTitle(string title) => this.SetTitle(title);
    IDirectoryBuilder IDirectoryBuilder.SetTotalMatchingItems(int totalMatchingItems) => this.SetTotalMatchingItems(totalMatchingItems);

    private DirectoryBuilder AddItem(IDirectoryItem item)
    {
        if (this.EntryCount == this.Parameters.Limit)
        {
            throw new InvalidOperationException("Can not add more entries");
        }
        this._items.Add(item);
        if (item is { Type: DirectoryItemType.Entry })
        {
            this.EntryCount++;
        }
        return this;
    }

    private static DirectoryButtonData ToData(DirectoryButton button) => new(
        button.Text is { Length: > 0 } text ? text : throw new ArgumentException("Button text must not be null or empty.", nameof(button)),
        button.Icon,
        button.Inverse,
        button.ActionIdentifier,
        button.UIAction
    );

    private static DirectoryEntryData ToData(DirectoryEntry entry) => new(
        entry.Title,
        entry.Label,
        entry.BrowseIdentifier,
        Validator.ValidateThumbnailUri(entry.ThumbnailUri),
        entry.IsQueueable,
        entry.ActionIdentifier,
        entry.UIAction
    );

    private static DirectoryInfoItemData ToData(DirectoryInfoItem infoItem) => new(
        infoItem.Title is { Length: > 0 } title ? title : throw new ArgumentException("Info item title must not be null or empty.", nameof(infoItem)),
        infoItem.Text is { Length: > 0 } text ? text : throw new ArgumentException("Info item text must not be null or empty.", nameof(infoItem)),
        infoItem.ActionIdentifier,
        infoItem.AffirmativeButtonText,
        infoItem.NegativeButtonText
    );

    private static DirectoryTileData ToData(DirectoryTile tile) => new(
        Validator.ValidateThumbnailUri(tile.ThumbnailUri, required: true)!,
        tile.ActionIdentifier,
        tile.UIAction
    );
}
