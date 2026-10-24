namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Basic information regarding a directory page.
/// </summary>
public readonly struct DirectoryPageInfo(BrowseParameters parameters, int offset, int? limit = null)
{
    /// <summary>
    /// Gets the browse identifier.
    /// </summary>
    public string BrowseIdentifier => parameters.BrowseIdentifier;

    /// <summary>
    /// Gets the maximum page size limit.
    /// </summary>
    public int Limit => limit ?? parameters.Limit;

    /// <summary>
    /// Gets the offset if this is not the first page (otherwise 0).
    /// </summary>
    public int Offset => offset;
}
