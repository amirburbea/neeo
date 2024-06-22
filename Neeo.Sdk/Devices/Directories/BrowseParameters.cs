namespace Neeo.Sdk.Devices.Directories;

/// <summary>
/// Parameters for a directory browse operation.
/// </summary>
/// <param name="BrowseIdentifier">Identifier for the current directory to browse.</param>
/// <param name="Limit">The expected limit of the number of items to provide.</param>
/// <param name="Offset">The pagination offset to use.</param>
public readonly record struct BrowseParameters(
    string? BrowseIdentifier = default,
    int Limit = Constants.MaxItems,
    int? Offset = default
);
