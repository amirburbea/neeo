namespace Neeo.Sdk.Devices.Lists;

public record struct ListParameters(
    string? BrowseIdentifier = default,
    int Limit = Constants.MaxItems,
    int? Offset = default
);