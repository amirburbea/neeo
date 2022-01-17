using System;
using System.Collections.Generic;
using System.Net;
using Neeo.Api.Devices.Components;
using Neeo.Api.Devices.Sensors;

namespace Neeo.Api.Devices;

public interface ICapabilityHandler
{
    ComponentType ComponentType { get; }

    IComponentController Controller { get; }
}

internal sealed record class CapabilityHandler(ComponentType ComponentType, IComponentController Controller) : ICapabilityHandler;

internal static class CapabilityHandlers
{
    public static (IReadOnlyDictionary<string, ICapabilityHandler>, IReadOnlyCollection<IComponent>) Build(IDeviceBuilder builder)
    {
        string deviceIdentifier = (builder ?? throw new ArgumentNullException(nameof(builder))).AdapterName;
        Dictionary<string, ICapabilityHandler> handlers = new();
        List<IComponent> components = new();
        HashSet<string> paths = new();

        void AddCapability(IComponent component, IComponentController? controller)
        {
            if (controller == null)
            {
                throw new ArgumentNullException(nameof(controller));
            }
            AddRouteHandler(component, controller);
            components.Add(component);
        }

        void AddRouteHandler(IComponent component, IComponentController controller)
        {
            if (!paths.Add(component.Path))
            {
                throw new InvalidOperationException($"Path {component.Path} is not unique.");
            }
            handlers[WebUtility.UrlDecode(component.Name)] = new CapabilityHandler(component.Type, controller);
        }

        if (builder.DiscoveryProcessor is { } processor)
        {
            AddRouteHandler(ComponentFactory.CreateDiscovery(deviceIdentifier), ComponentController.Create(processor));
        }
        foreach (DeviceFeature feature in builder.Buttons)
        {
            AddCapability(ComponentFactory.CreateButton(deviceIdentifier, feature), feature.Controller);
        }
        foreach (DeviceFeature feature in builder.Sliders)
        {
            ISliderComponent component = ComponentFactory.CreateSlider(deviceIdentifier, feature);
            AddCapability(ComponentFactory.CreateAssociatedRangeSensor(component, feature), feature.Controller);
            AddCapability(component, feature.Controller);
        }
        foreach (DeviceFeature feature in builder.Switches)
        {
            ISwitchComponent component = ComponentFactory.CreateSwitch(deviceIdentifier, feature);
            AddCapability(ComponentFactory.CreateAssociatedSensor(component, SensorType.Binary), feature.Controller);
            AddCapability(component, feature.Controller);
        }
        foreach (DeviceFeature feature in builder.TextLabels)
        {
            ITextLabelComponent component = ComponentFactory.CreateTextLabel(deviceIdentifier, feature);
            AddCapability(ComponentFactory.CreateAssociatedSensor(component, SensorType.String), feature.Controller);
            AddCapability(component, feature.Controller);
        }
        foreach (DeviceFeature feature in builder.ImageUrls)
        {
            IImageUrlComponent component = ComponentFactory.CreateImageUrl(deviceIdentifier, feature);
            AddCapability(ComponentFactory.CreateAssociatedSensor(component, SensorType.String), feature.Controller);
            AddCapability(component, feature.Controller);
        }
        foreach (DeviceFeature feature in builder.Sensors)
        {
            AddCapability(
                feature.Type == ComponentType.Power ? ComponentFactory.CreatePowerSensor(deviceIdentifier, feature) : ComponentFactory.CreateRangeSensor(deviceIdentifier, feature),
                feature.Controller
            );
        }

        return (handlers, components);
    }
}