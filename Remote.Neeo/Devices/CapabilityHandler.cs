using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Remote.Neeo.Devices.Components;
using Remote.Neeo.Devices.Sensors;

namespace Remote.Neeo.Devices
{
    public interface ICapabilityHandler
    {
        ComponentType ComponentType { get; }

        IComponentController Controller { get; }
    }

    internal sealed class CapabilityHandler : ICapabilityHandler
    {
        private CapabilityHandler(ComponentType componentType, IComponentController controller)
        {
            this.ComponentType = componentType;
            this.Controller = controller;
        }

        public ComponentType ComponentType { get; }

        public IComponentController Controller { get; }

        public static (IReadOnlyDictionary<string, ICapabilityHandler>, IReadOnlyCollection<IComponent>) Build(IDeviceBuilder builder)
        {
            string deviceIdentifier = (builder ?? throw new ArgumentNullException(nameof(builder))).AdapterName;
            Dictionary<string, ICapabilityHandler> handlers = new();
            List<IComponent> components = new();

            bool IsPathUnique(string path) => components.All(component => component.Path != path);

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
                if (!IsPathUnique(component.Path))
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

            // directories

            foreach (DeviceFeature feature in builder.Sensors)
            {
                Func<string, DeviceFeature, ISensorComponent> createSensor = feature.Type == ComponentType.Power
                    ? ComponentFactory.CreatePowerSensor
                    : ComponentFactory.CreateRangeSensor;
                AddCapability(createSensor(deviceIdentifier, feature), feature.Controller);
            }

            return (handlers, components);
        }
    }
}