namespace Neeo.Sdk.Devices.Lists;

public record struct ListParameters(
    string? BrowseIdentifier,
    int Limit = Constants.MaxItems,
    int? Offset = default,
    string? Title = default,
    int? TotalMatchingItems = default
);