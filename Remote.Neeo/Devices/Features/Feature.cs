using System;

namespace Remote.Neeo.Devices.Features
{
    internal abstract class Feature : IFeature, ISensorFeature, IImageUrlFeature
    {
        protected Feature(ComponentType type, string name, string? label, double? rangeLow, double? rangeHigh, string? units, ImageSize? size)
        {
            this.ComponentType = type;
            Validator.ValidateString(this.Name = name, prefix: nameof(name));
            Validator.ValidateString(this.Label = label, allowNull: true, prefix: nameof(label));
            if (type == ComponentType.Slider || type == ComponentType.Sensor)
            {
                Validator.ValidateRange((this.RangeLow = rangeLow).GetValueOrDefault(), (this.RangeHigh = rangeHigh).GetValueOrDefault(), this.Units = units);
            }
            this.Size = size;
        }

        public ComponentType ComponentType { get; }

        public virtual IDeviceValueController? Controller { get; set; }

        public string? Label { get; }

        public string Name { get; }

        public double? RangeHigh { get; }

        double ISensorFeature.RangeHigh => this.RangeHigh ?? default;

        public double? RangeLow { get; }

        double ISensorFeature.RangeLow => this.RangeLow ?? default;

        public ImageSize? Size { get; }

        ImageSize IImageUrlFeature.Size => this.Size ?? default;

        public string? Units { get; }

        string ISensorFeature.Units => this.Units ?? string.Empty;
    }

    internal sealed class Feature<TValue> : Feature
        where TValue : notnull, IConvertible
    {
        public Feature(ComponentType type, string name, string? label, double? rangeLow = default, double? rangeHigh = default, string? units = default, ImageSize? size = default)
            : base(type, name, label, rangeLow, rangeHigh, units, size)
        {
        }

        public override DeviceValueController<TValue>? Controller => (DeviceValueController<TValue>?)base.Controller;
    }
}