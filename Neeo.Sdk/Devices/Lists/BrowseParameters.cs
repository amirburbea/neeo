namespace Neeo.Sdk.Devices.Lists;

public record struct BrowseParameters(
    string? BrowseIdentifier = default,
    int Limit = Constants.MaxItems,
    int? Offset = default
);