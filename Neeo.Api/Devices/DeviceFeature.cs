namespace Neeo.Api.Devices;

public interface IDeviceFeature
{
    IComponentController Controller { get; }

    bool? IsLabelVisible { get; }

    string? Label { get; }

    string Name { get; }

    double? RangeHigh { get; }

    double? RangeLow { get; }

    string? SensorLabel { get; }

    SensorTypes? SensorType { get; }

    ImageSize? Size { get; }

    ComponentType Type { get; }

    string? Unit { get; }

    string? Uri { get; }
}

internal sealed record class DeviceFeature : IDeviceFeature
{
    public DeviceFeature(
        ComponentType type,
        string name,
        string? label = default,
        bool? isLabelVisible = default,
        double? rangeLow = default,
        double? rangeHigh = default,
        string? unit = default,
        string? uri = default,
        ImageSize? size = default,
        SensorTypes? sensorType = default,
        string? sensorLabel = default
    )
    {
        (this.Type, this.Size, this.Uri, this.IsLabelVisible) = (type, size, uri, isLabelVisible);
        Validator.ValidateString(this.Name = name, name: nameof(name));
        Validator.ValidateString(this.Label = label, allowNull: true, name: nameof(label));
        Validator.ValidateString(this.SensorLabel = sensorLabel, allowNull: true, name: nameof(sensorLabel));
        if (type is ComponentType.Slider or ComponentType.Sensor)
        {
            Validator.ValidateString(this.Unit = unit ?? "%", name: nameof(unit));
            if ((this.SensorType = sensorType ?? SensorTypes.Range) == SensorTypes.Range)
            {
                Validator.ValidateRange((double)(this.RangeLow = rangeLow ?? 0d), (double)(this.RangeHigh = rangeHigh ?? 100d));
            }
        }
    }

    public IComponentController? Controller { get; set; }

    public bool? IsLabelVisible { get; init; }

    public string? Label { get; init; }

    public string Name { get; init; }

    public double? RangeHigh { get; init; }

    public double? RangeLow { get; init; }

    public string? SensorLabel { get; init; }

    public SensorTypes? SensorType { get; init; }

    public ImageSize? Size { get; init; }

    public ComponentType Type { get; init; }

    public string? Unit { get; init; }

    public string? Uri { get; init; }

    IComponentController IDeviceFeature.Controller => this.Controller!;
}