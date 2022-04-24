namespace Neeo.Sdk.Devices.Lists;

public readonly record struct BrowseParameters(
    string? BrowseIdentifier = default,
    int Limit = Constants.MaxItems,
    int? Offset = default
);