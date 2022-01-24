namespace Neeo.Api.Devices;

public record struct DeviceFeature(
    ComponentType Type,
    string Name,
    string? Label = default,
    bool? IsLabelVisible = default,
    double? RangeLow = default,
    double? RangeHigh = default,
    string? Unit = default,
    string? Uri = default,
    ImageSize? Size = default,
    SensorTypes? SensorType = default,
    string? SensorLabel = default
);