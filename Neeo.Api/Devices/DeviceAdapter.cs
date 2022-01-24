using System;
using System.Collections.Generic;
using System.Linq;
using Neeo.Api.Devices.Components;
using Neeo.Api.Devices.Controllers;
using Neeo.Api.Utilities;

namespace Neeo.Api.Devices;

using static KnownButtons;

public interface IDeviceAdapter
{
    string AdapterName { get; }

    IReadOnlyCollection<IComponent> Capabilities { get; }

    IReadOnlyDictionary<string, IController> CapabilityHandlers { get; }

    IReadOnlyCollection<DeviceCapability> DeviceCapabilities { get; }

    string DeviceName { get; }

    uint? DriverVersion { get; }

    DeviceIconOverride? Icon { get; }

    DeviceInitializer? Initializer { get; }

    string Manufacturer { get; }

    IDeviceSetup Setup { get; }

    string? SpecificName { get; }

    DeviceTiming Timing { get; }

    IReadOnlyCollection<string> Tokens { get; }

    DeviceType Type { get; }

    IController? GetCapabilityHandler(ComponentType type);

    IController? GetCapabilityHandler(string name);
}

internal record DeviceAdapter(
    string AdapterName,
    IReadOnlyCollection<IComponent> Capabilities,
    IReadOnlyDictionary<string, IController> CapabilityHandlers,
    IReadOnlyCollection<DeviceCapability> DeviceCapabilities,
    string DeviceName,
    uint? DriverVersion,
    DeviceIconOverride? Icon,
    DeviceInitializer? Initializer,
    string Manufacturer,
    IDeviceSetup Setup,
    string? SpecificName,
    DeviceTiming Timing,
    IReadOnlyCollection<string> Tokens,
    DeviceType Type
) : IDeviceAdapter
{
    public IController? GetCapabilityHandler(ComponentType type) => this.GetCapabilityHandler(TextAttribute.GetText(type));

    public IController? GetCapabilityHandler(string name) => this.CapabilityHandlers.GetValueOrDefault(name);

    public static DeviceAdapter Build(IDeviceBuilder device, DiscoveryControllerFactory discoveryControllerFactory)
    {
        if (device.ButtonHandler == null && device.Buttons.Any())
        {
            throw new InvalidOperationException($"There are buttons defined but no handler was specified (by calling {nameof(IDeviceBuilder.AddButtonHandler)}.");
        }
        if (device.Type.RequiresInput() && !device.Buttons.Any(button => button.Name.StartsWith(Constants.InputPrefix)))
        {
            throw new InvalidOperationException($"No input buttons defined - note that input button names must begin with \"{Constants.InputPrefix}\".");
        }
        if (device.Characteristics.Contains(DeviceCharacteristic.BridgeDevice) && device.Setup.RegistrationType is null)
        {
            throw new InvalidOperationException($"A device with characteristic {DeviceCharacteristic.BridgeDevice} must support registration (by calling {nameof(IDeviceBuilder.EnableRegistration)}).");
        }
        List<DeviceCapability> deviceCapabilities = device.Characteristics.Select(characteristic => (DeviceCapability)characteristic).ToList();
        string pathPrefix = $"/device/{device.AdapterName}/";
        HashSet<string> paths = new();
        List<Component> capabilities = new();
        Dictionary<string, IController> handlers = new();
        int digitCount = 0;
        foreach (DeviceFeature feature in device.Buttons)
        {
            AddCapability(BuildButton(pathPrefix, feature), new ButtonController(device.ButtonHandler!, feature.Name));
            if (device.FavoritesController != null && KnownButton.GetKnownButton(feature.Name) is Digit0 or Digit1 or Digit2 or Digit3 or Digit4 or Digit5 or Digit6 or Digit7 or Digit8 or Digit9)
            {
                digitCount++;
            }
        }
        foreach ((DeviceFeature feature, IValueController controller) in device.Sliders)
        {
            AddCapability(BuildSensor(pathPrefix, feature with { Type = ComponentType.Sensor, SensorType = SensorTypes.Range }), controller);
            AddCapability(BuildSlider(pathPrefix, feature), controller);
        }
        foreach ((DeviceFeature feature, IValueController controller) in device.Switches)
        {
            AddCapability(BuildSensor(pathPrefix, feature with { Type = ComponentType.Sensor, SensorType = SensorTypes.Binary }), controller);
            AddCapability(BuildSwitch(pathPrefix, feature), controller);
        }
        foreach ((DeviceFeature feature, IValueController controller) in device.TextLabels)
        {
            AddCapability(BuildSensor(pathPrefix, feature with { Type = ComponentType.Sensor, SensorType = SensorTypes.String }), controller);
            AddCapability(BuildTextLabel(pathPrefix, feature), controller);
        }
        foreach ((DeviceFeature feature, IValueController controller) in device.ImageUrls)
        {
            AddCapability(BuildSensor(pathPrefix, feature with { Type = ComponentType.Sensor, SensorType = SensorTypes.String }), controller);
            AddCapability(BuildImageUrl(pathPrefix, feature), controller);
        }
        foreach ((DeviceFeature feature, IValueController controller) in device.Sensors)
        {
            AddCapability(feature.SensorType == SensorTypes.Power ? BuildPowerSensor(pathPrefix, feature) : BuildSensor(pathPrefix, feature), controller);
        }
        if (device.DiscoveryProcess is { } discoveryProcess)
        {
            AddRouteHandler(BuildComponent(pathPrefix, ComponentType.Discovery), discoveryControllerFactory.CreateController(discoveryProcess, device.Setup.EnableDynamicDeviceBuilder ?? false));
            if (device.RegistrationController is { } registrationController)
            {
                deviceCapabilities.Add(DeviceCapability.RegisterUserAccount);
                AddRouteHandler(BuildComponent(pathPrefix, ComponentType.Registration), registrationController);
            }
        }
        else if (!deviceCapabilities.Contains(DeviceCapability.DynamicDevice) && deviceCapabilities.FindIndex(RequiresDiscovery) is int index and > -1)
        {
            throw new InvalidOperationException($"Discovery required for {deviceCapabilities[index]}.");
        }
        if (device.FavoritesController is { } favoritesController)
        {
            if (digitCount != 10)
            {
                throw new InvalidOperationException("Can not enable favorites without the 10 digit buttons being added. It is highly recommended to call AddButtonGroup(ButtonGroup.NumberPad).");
            }
            deviceCapabilities.Add(DeviceCapability.CustomFavoriteHandler);
            AddRouteHandler(BuildComponent(pathPrefix, ComponentType.FavoritesHandler), favoritesController);
        }
        if (device.SubscriptionController is { } subscriptionController)
        {
            AddRouteHandler(BuildComponent(pathPrefix, ComponentType.Subscription), subscriptionController);
        }
        return new(
            device.AdapterName,
            capabilities,
            handlers,
            deviceCapabilities,
            device.Name,
            device.DriverVersion,
            device.Icon,
            device.Initializer,
            device.Manufacturer,
            device.Setup,
            device.SpecificName,
            device.Timing ?? new(),
            device.AdditionalSearchTokens,
            device.Type
        );

        void AddCapability(Component capability, IController controller)
        {
            AddRouteHandler(capability, controller);
            capabilities.Add(capability);
        }

        void AddRouteHandler(Component capability, IController controller)
        {
            handlers[Uri.UnescapeDataString(capability.Name)] = paths.Add(capability.Path)
                ? controller
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
                    ? new RangeSensorDescriptor(feature.RangeLow ?? 0d, feature.RangeHigh ?? 100d, feature.Unit)
                    : new SensorDescriptor(sensorType)
            );
        }

        static SliderComponent BuildSlider(string pathPrefix, DeviceFeature feature)
        {
            string name = Uri.EscapeDataString(feature.Name);
            string path = pathPrefix + name;
            string label = feature.Label is { } text ? Uri.EscapeDataString(text) : name;
            return new(name, label, path, new(new[] { feature.RangeLow ?? 0d, feature.RangeHigh ?? 100d }, feature.Unit!, GetSensorName(name)));
        }

        static SwitchComponent BuildSwitch(string pathPrefix, DeviceFeature feature)
        {
            string name = Uri.EscapeDataString(feature.Name);
            string path = pathPrefix + name;
            string label = Uri.EscapeDataString(feature.Label ?? string.Empty);
            return new(name, label, path, GetSensorName(name));
        }

        static string GetSensorName(string name) => name.EndsWith(SensorDescriptor.ComponentSuffix) ? name : string.Concat(name.ToUpperInvariant(), SensorDescriptor.ComponentSuffix);

        static bool RequiresDiscovery(DeviceCapability capability) => capability is DeviceCapability.BridgeDevice or DeviceCapability.AddAnotherDevice or DeviceCapability.RegisterUserAccount;
    }
}