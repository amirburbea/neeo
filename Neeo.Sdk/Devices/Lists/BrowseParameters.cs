namespace Neeo.Sdk.Devices.Lists;

/// <summary>
/// Parameters structure for initializing a <see cref="ListBuilder"/>.
/// </summary>
/// <param name="BrowseIdentifier">The identifier of the directory to browse. A value of <see langword="null"/> or blank typically indicates the root.</param>
/// <param name="Limit">The maximum number of entries that should be returned from this browse operation.</param>
/// <param name="Offset">When not <see langword="null"/> or 0, </param>
public readonly record struct BrowseParameters(
    string? BrowseIdentifier = default,
    int Limit = Constants.MaxItems,
    int? Offset = default
);
