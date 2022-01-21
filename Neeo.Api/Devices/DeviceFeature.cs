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

//        (this.Type, this.Size, this.Uri, this.IsLabelVisible) = (type, size, uri, isLabelVisible);
//        Validator.ValidateString(this.Name = name, name: nameof(name));
//        Validator.ValidateString(this.Label = label, allowNull: true, name: nameof(label));
//        Validator.ValidateString(this.SensorLabel = sensorLabel, allowNull: true, name: nameof(sensorLabel));
//        if (type is ComponentType.Slider or ComponentType.Sensor)
//        {
//            Validator.ValidateString(this.Unit = unit ?? "%", name: nameof(unit));
//            if ((this.SensorType = sensorType ?? SensorTypes.Range) == SensorTypes.Range)
//            {
//                Validator.ValidateRange((double)(this.RangeLow = rangeLow ?? 0d), (double)(this.RangeHigh = rangeHigh ?? 100d));
//            }
//        }
