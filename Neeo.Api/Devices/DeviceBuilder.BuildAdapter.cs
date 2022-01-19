using System;
using System.Collections.Generic;
using System.Linq;
using Neeo.Api.Devices.Components;
using Neeo.Api.Utilities;

namespace Neeo.Api.Devices;

internal partial class DeviceBuilder
{
    private DeviceAdapter BuildAdapter()
    {
        if (this.ButtonHandler == null && this.Buttons.Any())
        {
            throw new InvalidOperationException($"There are buttons defined but no handler was specified (by calling {nameof(IDeviceBuilder.AddButtonHandler)}.");
        }
        if (this.Type.RequiresInput() && !this.Buttons.Any(button => button.Name.StartsWith(Constants.InputPrefix)))
        {
            throw new InvalidOperationException($"No input buttons defined - note that input button names must begin with \"{Constants.InputPrefix}\".");
        }
        if (this._characteristics.Contains(DeviceCharacteristic.BridgeDevice) && this.Setup.RegistrationType is null)
        {
            throw new InvalidOperationException($"A device with characteristic {DeviceCharacteristic.BridgeDevice} must support registration (by calling {nameof(IDeviceBuilder.EnableRegistration)}).");
        }
        List<DeviceCapability> deviceCapabilities = this.Characteristics.Select(characteristic => (DeviceCapability)characteristic).ToList();
        if (this.RegistrationProcessor != null)
        {
            deviceCapabilities.Add(DeviceCapability.RegisterUserAccount);
        }
        if (this.FavoritesHandler != null)
        {
            deviceCapabilities.Add(DeviceCapability.CustomFavoriteHandler);
        }
        string pathPrefix = $"/device/{this.AdapterName}/";
        HashSet<string> paths = new();
        List<Component> capabilities = new();
        Dictionary<string, CapabilityHandler> handlers = new();
        foreach (DeviceFeature feature in this.Buttons)
        {
            AddCapability(BuildButton(pathPrefix, feature), ComponentController.Create(this.ButtonHandler!, feature.Name));
        }
        foreach (DeviceFeature feature in this.Sliders)
        {
            AddCapability(BuildSensor(pathPrefix, feature with { SensorType = SensorTypes.Range }), feature.Controller);
            AddCapability(BuildSlider(pathPrefix, feature), feature.Controller);
        }
        foreach (DeviceFeature feature in this.Switches)
        {
            AddCapability(BuildSensor(pathPrefix, feature with { Type = ComponentType.Sensor, SensorType = SensorTypes.Binary }), feature.Controller);
            AddCapability(BuildSwitch(pathPrefix, feature), feature.Controller);
        }
        foreach (DeviceFeature feature in this.TextLabels)
        {
            AddCapability(BuildSensor(pathPrefix, feature with { Type = ComponentType.Sensor, SensorType = SensorTypes.String }), feature.Controller);
            AddCapability(BuildTextLabel(pathPrefix, feature), feature.Controller);
        }
        foreach (DeviceFeature feature in this.ImageUrls)
        {
            AddCapability(BuildSensor(pathPrefix, feature with { Type = ComponentType.Sensor, SensorType = SensorTypes.String }), feature.Controller);
            AddCapability(BuildImageUrl(pathPrefix, feature), feature.Controller);
        }
        foreach (DeviceFeature feature in this.Sensors)
        {
            AddCapability(feature.SensorType == SensorTypes.Power ? BuildPowerSensor(pathPrefix, feature) : BuildSensor(pathPrefix, feature), feature.Controller);
        }
        if (this.DiscoveryProcessor is not null)
        {
            AddRouteHandler(BuildComponent(pathPrefix, ComponentType.Discovery), ComponentController.Create(this.DiscoveryProcessor));
            if (this.RegistrationProcessor is not null)
            {
                //AddRouteHandler(BuildComponent(pathPrefix, ComponentType.Registration), ComponentController.Create(this.QueryIsRegistered, this.RegistrationProcessor));
            }
        }
        else if (!this.Characteristics.Contains(DeviceCharacteristic.DynamicDevice) && deviceCapabilities.FindIndex(static capability => capability.RequiresDiscovery()) is int index and not -1)
        {
            throw new InvalidOperationException($"Discovery required for {deviceCapabilities[index]}.");
        }
        if (this.FavoritesHandler is not null)
        {
            AddRouteHandler(BuildComponent(pathPrefix, ComponentType.FavoritesHandler), ComponentController.Create(this.FavoritesHandler));
        }
        if (this.DeviceSubscriptionCallbacks is not null)
        {
            //AddRouteHandler(BuildComponent(pathPrefix, ComponentType.Subscription), ComponentController.Create(this.DeviceSubscriptionCallbacks));
        }
        return new(
            this.AdapterName,
            this.Type,
            this.Manufacturer,
            this.DriverVersion,
            this.Timing,
            deviceCapabilities,
            this.Setup,
            this.Initializer,
            this.Name,
            this.AdditionalSearchTokens,
            this.SpecificName,
            this.Icon,
            capabilities,
            new CovariantReadOnlyDictionary<string, CapabilityHandler, ICapabilityHandler>(handlers)
        );

        void AddCapability(Component capability, IComponentController? controller)
        {
            AddRouteHandler(capability, controller ?? throw new ArgumentNullException(nameof(controller)));
            capabilities.Add(capability);
        }

        void AddRouteHandler(Component capability, IComponentController controller)
        {
            handlers[Uri.UnescapeDataString(capability.Name)] = paths.Add(capability.Path)
                ? new(capability.Type, controller)
                : throw new InvalidOperationException($"Path {capability.Path} is not unique.");
        }

        static Component BuildButton(string pathPrefix, DeviceFeature feature)
        {
            string name = Uri.EscapeDataString(feature.Name);
            string path = pathPrefix + name;
            string label = feature.Label is { } text ? Uri.EscapeDataString(text) : name;
            return new(ComponentType.Button, name, label, path);
        }

        static Component BuildComponent(string pathPrefix, ComponentType type)
        {
            string name = Uri.EscapeDataString(TextAttribute.GetText(type));
            string path = pathPrefix + name;
            return new(type, name, default, path);
        }

        static ImageUrlComponent BuildImageUrl(string pathPrefix, DeviceFeature feature)
        {
            string name = Uri.EscapeDataString(feature.Name);
            string path = pathPrefix + name;
            string label = feature.Label is { } text ? Uri.EscapeDataString(text) : name;
            return new(name, label, path, feature.Uri, feature.Size ?? ImageSize.Large, GetSensorName(name));
        }

        static SensorComponent BuildPowerSensor(string pathPrefix, DeviceFeature feature)
        {
            SensorComponent component = BuildSensor(pathPrefix, feature);
            /*
                Power state sensors are added by AddPowerStateSensor with the name "powerstate".
                For backward compatibility, we need to avoid changing it to "POWERSTATE_SENSOR".
            */
            string legacyNoSuffixName = Uri.EscapeDataString(feature.Name);
            return component with { Name = legacyNoSuffixName, Path = pathPrefix + legacyNoSuffixName };
        }

        static TextLabelComponent BuildTextLabel(string pathPrefix, DeviceFeature feature)
        {
            string name = Uri.EscapeDataString(feature.Name);
            string path = pathPrefix + name;
            string label = feature.Label is { } text ? Uri.EscapeDataString(text) : name;
            return new(name, label, path, feature.IsLabelVisible, GetSensorName(name));
        }

        static SensorComponent BuildSensor(string pathPrefix, DeviceFeature feature)
        {
            string name = GetSensorName(Uri.EscapeDataString(feature.Name));
            string path = pathPrefix + name;
            string label = Uri.EscapeDataString(feature.SensorLabel ?? feature.Label ?? feature.Name);
            SensorTypes sensorType = feature.SensorType ?? SensorTypes.Range;
            return new(
                name,
                label,
                path,
                sensorType is SensorTypes.Range
                    ? new RangeSensor(feature.RangeLow ?? 0d, feature.RangeHigh ?? 100d, feature.Unit)
                    : new Sensor(sensorType)
            );
        }

        static SliderComponent BuildSlider(string pathPrefix, DeviceFeature feature)
        {
            string name = Uri.EscapeDataString(feature.Name);
            string path = pathPrefix + name;
            string label = feature.Label is { } text ? Uri.EscapeDataString(text) : name;
            return new(name, label, path, new Slider(new[] { feature.RangeLow ?? 0d, feature.RangeHigh ?? 100d }, feature.Unit!, GetSensorName(name)));
        }

        static SwitchComponent BuildSwitch(string pathPrefix, DeviceFeature feature)
        {
            string name = Uri.EscapeDataString(feature.Name);
            string path = pathPrefix + name;
            string label = Uri.EscapeDataString(feature.Label ?? string.Empty);
            return new(name, label, path, GetSensorName(name));
        }

        static string GetSensorName(string name) => name.EndsWith(Sensor.ComponentSuffix) ? name : string.Concat(name.ToUpperInvariant(), Sensor.ComponentSuffix);
    }
}