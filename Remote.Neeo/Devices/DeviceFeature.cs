using Remote.Neeo.Devices.Components;

namespace Remote.Neeo.Devices
{
    public sealed class DeviceFeature
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

        public IComponentController? Controller { get; set; }

        public bool? IsLabelVisible { get; }

        public string? Label { get; }

        public string Name { get; }

        public double? RangeHigh { get; }

        public double? RangeLow { get; }

        public ImageSize? Size { get; }

        public ComponentType Type { get; }

        public string? Unit { get; }
    }
}