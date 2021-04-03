using System;
using Remote.Neeo.Devices.Discovery;
using Remote.Neeo.Devices.Sensors;

namespace Remote.Neeo.Devices.Components
{
    internal static class ComponentFactory
    {
        public static SensorComponent CreateAssociatedRangeSensor(IComponentWithAssociatedSensor component, DeviceFeature feature) => new(
            component.SensorName,
            component.Label,
            ComponentFactory.ExtractPathPrefix(component),
            new RangeSensor(feature.RangeLow ?? default, feature.RangeHigh ?? default, feature.Unit)
        );

        public static SensorComponent CreateAssociatedSensor(IComponentWithAssociatedSensor component, SensorType sensorType) => new(
            component.SensorName,
            component.Label,
            ComponentFactory.ExtractPathPrefix(component),
            new(sensorType)
        );

        public static ButtonComponent CreateButton(string deviceIdentifier, DeviceFeature feature) => new(
            Uri.EscapeDataString(feature.Name),
            feature.Label,
            ComponentFactory.GetPathPrefix(deviceIdentifier)
        );

        public static DiscoveryComponent CreateDiscovery(string deviceIdentifier) => new(            
            ComponentFactory.GetPathPrefix(deviceIdentifier)
        );

        public static ImageUrlComponent CreateImageUrl(string deviceIdentifier, DeviceFeature feature) => new(
            Uri.EscapeDataString(feature.Name),
            feature.Label,
            ComponentFactory.GetPathPrefix(deviceIdentifier),
            feature.Size ?? default
        );

        public static SensorComponent CreatePowerSensor(string deviceIdentifier, DeviceFeature feature) => new(
            Uri.EscapeDataString(feature.Name),
            feature.Label,
            ComponentFactory.GetPathPrefix(deviceIdentifier),
            new(SensorType.Power)
        );

        public static SensorComponent CreateRangeSensor(string deviceIdentifier, DeviceFeature feature) => new(
            Uri.EscapeDataString(feature.Name),
            feature.Label,
            ComponentFactory.GetPathPrefix(deviceIdentifier),
            new RangeSensor(feature.RangeLow ?? default, feature.RangeHigh ?? default, feature.Unit)
        );

        public static SliderComponent CreateSlider(string deviceIdentifier, DeviceFeature feature) => new(
            Uri.EscapeDataString(feature.Name),
            feature.Label,
            ComponentFactory.GetPathPrefix(deviceIdentifier)
        );

        public static SwitchComponent CreateSwitch(string deviceIdentifier, DeviceFeature feature) => new(
            Uri.EscapeDataString(feature.Name),
            feature.Label,
            ComponentFactory.GetPathPrefix(deviceIdentifier)
        );

        public static TextLabelComponent CreateTextLabel(string deviceIdentifier, DeviceFeature feature) => new(
            Uri.EscapeDataString(feature.Name),
            feature.Label,
            ComponentFactory.GetPathPrefix(deviceIdentifier),
            feature.IsLabelVisible
        );

        private static string ExtractPathPrefix(IComponent component) => component.Path[..^component.Name.Length];

        private static string GetPathPrefix(string deviceIdentifier) => $"/device/{deviceIdentifier}/";
    }
}