using Neeo.Api.Devices.Components;

namespace Neeo.Api.Devices;

public interface IDeviceFeature
{
    IComponentController Controller { get; }

    bool? IsLabelVisible { get; }

    string? Label { get; }

    string Name { get; }

    double? RangeHigh { get; }

    double? RangeLow { get; }

    ImageSize? Size { get; }

    ComponentType Type { get; }

    string? Unit { get; }
}

public sealed class DeviceFeature : IDeviceFeature
{
    public DeviceFeature(ComponentType type, string name, string? label = default, bool? isLabelVisible = default, double? rangeLow = default, double? rangeHigh = default, string? unit = default, ImageSize? size = default)
    {
        this.Type = type;
        Validator.ValidateString(this.Name = name, name: nameof(name));
        Validator.ValidateString(this.Label = label, allowNull: true, name: nameof(label));
        if (type == ComponentType.Slider || type == ComponentType.Sensor)
        {
            Validator.ValidateRange((this.RangeLow = rangeLow).GetValueOrDefault(), (this.RangeHigh = rangeHigh).GetValueOrDefault(), this.Unit = unit);
        }
        this.Size = size;
        this.IsLabelVisible = isLabelVisible;
    }

    public bool? IsLabelVisible { get; }

    public string? Label { get; }

    public string Name { get; }

    public double? RangeHigh { get; }

    public double? RangeLow { get; }

    public ImageSize? Size { get; }

    public ComponentType Type { get; }

    public string? Unit { get; }

    public IComponentController? Controller { get; set; }

    IComponentController IDeviceFeature.Controller => this.Controller!;
}