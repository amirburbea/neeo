namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Parameters for a directory browse operation.
/// </summary>
/// <param name="BrowseIdentifier">Identifier for the current directory to browse.</param>
/// <param name="Offset">The pagination offset to use.</param>
/// <param name="Limit">The expected limit of the number of items to provide.</param>
public readonly record struct BrowseParameters(
    string? BrowseIdentifier = null,
    int? Offset = null,
    int Limit = Constants.MaxItems
);
