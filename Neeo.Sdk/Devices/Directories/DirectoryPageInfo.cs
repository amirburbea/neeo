namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Basic information regarding a directory page.
/// </summary>
public readonly struct DirectoryPageInfo(DirectoryBuilder directory, int offset)
{
    /// <summary>
    /// Gets the browse identifier.
    /// </summary>
    public string? BrowseIdentifier => directory.BrowseIdentifier;

    /// <summary>
    /// Gets the maximum page size limit.
    /// </summary>
    public int Limit => directory.Parameters.Limit;

    /// <summary>
    /// Gets the offset if this is not the first page (otherwise 0).
    /// </summary>
    public int Offset => offset;
}
