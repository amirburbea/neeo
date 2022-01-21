using System;
using System.Collections.Generic;
using System.Linq;
using Neeo.Api.Devices.Components;
using Neeo.Api.Devices.Controllers;
using Neeo.Api.Utilities;

namespace Neeo.Api.Devices;

public sealed class DeviceCompiler
{
    

    internal DeviceAdapter BuildAdapter(IDeviceBuilder device)
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
        if (device.RegistrationProcessor != null)
        {
            deviceCapabilities.Add(DeviceCapability.RegisterUserAccount);
        }
        if (device.FavoritesHandler != null)
        {
            deviceCapabilities.Add(DeviceCapability.CustomFavoriteHandler);
        }
        string pathPrefix = $"/device/{device.AdapterName}/";
        HashSet<string> paths = new();
        List<Component> capabilities = new();
        Dictionary<string, CapabilityHandler> handlers = new();
        foreach (DeviceFeature feature in device.Buttons)
        {
            AddCapability(BuildButton(pathPrefix, feature), new ButtonController(device.ButtonHandler!, feature.Name));
        }
        foreach ((DeviceFeature feature, IController controller) in device.Sliders)
        {
            AddCapability(BuildSensor(pathPrefix, feature with { Type = ComponentType.Sensor, SensorType = SensorTypes.Range }), controller);
            AddCapability(BuildSlider(pathPrefix, feature), controller);
        }
        foreach ((DeviceFeature feature, IController controller) in device.Switches)
        {
            AddCapability(BuildSensor(pathPrefix, feature with { Type = ComponentType.Sensor, SensorType = SensorTypes.Binary }), controller);
            AddCapability(BuildSwitch(pathPrefix, feature), controller);
        }
        foreach ((DeviceFeature feature, IController controller) in device.TextLabels)
        {
            AddCapability(BuildSensor(pathPrefix, feature with { Type = ComponentType.Sensor, SensorType = SensorTypes.String }), controller);
            AddCapability(BuildTextLabel(pathPrefix, feature), controller);
        }
        foreach ((DeviceFeature feature, IController controller) in device.ImageUrls)
        {
            AddCapability(BuildSensor(pathPrefix, feature with { Type = ComponentType.Sensor, SensorType = SensorTypes.String }), controller);
            AddCapability(BuildImageUrl(pathPrefix, feature), controller);
        }
        foreach ((DeviceFeature feature, IController controller) in device.Sensors)
        {
            AddCapability(feature.SensorType == SensorTypes.Power ? BuildPowerSensor(pathPrefix, feature) : BuildSensor(pathPrefix, feature), controller);
        }
        if (device.DiscoveryProcessor is not null)
        {
           // AddRouteHandler(BuildComponent(pathPrefix, ComponentType.Discovery), ComponentController.Create(device.DiscoveryProcessor));
            if (device.RegistrationController is not null)
            {
                AddRouteHandler(BuildComponent(pathPrefix, ComponentType.Registration), device.RegistrationController);
            }
        }
        else if (!device.Characteristics.Contains(DeviceCharacteristic.DynamicDevice) && deviceCapabilities.FindIndex(static capability => capability.RequiresDiscovery()) is int index and not -1)
        {
            throw new InvalidOperationException($"Discovery required for {deviceCapabilities[index]}.");
        }
        if (device.FavoritesHandler is not null)
        {
            AddRouteHandler(BuildComponent(pathPrefix, ComponentType.FavoritesHandler), new FavoritesController(device.FavoritesHandler));
        }
        if (device.DeviceSubscriptionCallbacks is not null)
        {
            //AddRouteHandler(BuildComponent(pathPrefix, ComponentType.Subscription), ComponentController.Create(device.DeviceSubscriptionCallbacks));
        }
        return new(
            device.AdapterName,
            device.Type,
            device.Manufacturer,
            device.DriverVersion,
            device.Timing,
            deviceCapabilities,
            device.Setup,
            device.Initializer,
            device.Name,
            device.AdditionalSearchTokens,
            device.SpecificName,
            device.Icon,
            capabilities,
            new CovariantReadOnlyDictionary<string, CapabilityHandler, ICapabilityHandler>(handlers)
        );

        void AddCapability(Component capability, IController controller)
        {
            AddRouteHandler(capability, controller);
            capabilities.Add(capability);
        }

        void AddRouteHandler(Component capability, IController controller)
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
    }
}