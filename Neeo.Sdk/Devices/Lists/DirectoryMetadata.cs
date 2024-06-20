namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// Metadata relating to the current state of a <see cref="DirectoryBuilder"/>.
/// </summary>
/// <param name="directory">The directory instance.</param>
/// <param name="current">Information about the current page of data.</param>
/// <param name="previous">Information about the previous page of data if it exists (otherwise <see langword="null"/>).</param>
/// <param name="next">Information about the next page of data if it exists (otherwise <see langword="null"/>).</param>
public readonly struct DirectoryMetadata(DirectoryBuilder directory, ListPageInfo current, ListPageInfo? previous = null, ListPageInfo? next = null)
{
    /// <summary>
    /// Gets information about the current page of data.
    /// </summary>
    public ListPageInfo Current => current;

    /// <summary>
    /// In a paginated directory, gets information about the next page of data if it exists (otherwise <see langword="null"/>).
    /// </summary>
    public ListPageInfo? Next => next;

    /// <summary>
    /// In a paginated directory, gets information about the previous page of data if it exists (otherwise <see langword="null"/>).
    /// </summary>
    public ListPageInfo? Previous => previous;

    /// <summary>
    /// Gets the total number of items in the directory.
    /// </summary>
    public int TotalItems => directory.Items.Count;

    /// <summary>
    /// Gets the total matching item count for the query (including data not on the current page).
    /// </summary>
    public int? TotalMatchingItems => directory.TotalMatchingItems;
}
