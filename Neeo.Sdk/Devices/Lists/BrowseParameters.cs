namespace Neeo.Sdk.Devices.Lists;

public record struct BrowseParameters(
    string? BrowseIdentifier = default,
    int? Limit = default,
    int? Offset = default
);