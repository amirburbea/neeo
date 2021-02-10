using System;
using System.Collections.Generic;
using System.Linq;
using Remote.Neeo.Devices.Descriptors;
using Remote.Neeo.Devices.Discovery;

namespace Remote.Neeo.Devices
{
    /// <summary>
    /// Fluent interface for building device.
    /// </summary>
    public interface IDeviceBuilder
    {
        string AdapterName { get; }

        IReadOnlyCollection<string> AdditionalSearchTokens { get; }

        ButtonHandler? ButtonHandler { get; }

        IReadOnlyCollection<ButtonDescriptor> Buttons { get; }

        IReadOnlyCollection<DeviceCapability> Capabilities { get; }

        DelaysSpecifier? Delays { get; }

        uint? DriverVersion { get; }

        IFavoritesHandler? FavoritesHandler { get; }

        DeviceIcon? Icon { get; }

        IDeviceInitializer? Initializer { get; }

        string Manufacturer { get; }

        string Name { get; }

        DeviceValueGetter<bool>? PowerStateSensor { get; }

        IDeviceSetup Setup { get; }

        string? SpecificName { get; }

        DeviceType Type { get; }

        IDeviceBuilder AddAdditionalSearchToken(string text);

        IDeviceBuilder AddButton(ButtonDescriptor button);

        IDeviceBuilder AddButtonGroup(ButtonGroup group);

        IDeviceBuilder AddCapability(StaticDeviceCapability capability);

        IDeviceAdapter BuildAdapter();

        IDeviceBuilder EnableDiscovery(DiscoveryOptions options, DiscoveryController controller);

        IDeviceBuilder SetButtonHandler(ButtonHandler handler);

        IDeviceBuilder SetDelays(DelaysSpecifier delays);

        /// <summary>
        /// Setting the version allows you to tell the Brain about changes to your devices components. If you for example add new buttons to a device,
        /// you can increase the version and this will let the Brain know to fetch the new components.
        /// You do not need to update the version if you do not change the components. When adding a version to a device that was previously not versioned,
        /// start with 1. The Brain will assume it was previously 0 and update.
        /// <para />
        /// Note: The Brain will only add new components, updating or removing old components is not supported)
        /// </summary>
        IDeviceBuilder SetDriverVersion(uint version);

        IDeviceBuilder SetFavoritesHandler(IFavoritesHandler handler);

        IDeviceBuilder SetIcon(DeviceIcon icon);

        IDeviceBuilder SetInitializer(IDeviceInitializer initializer);

        IDeviceBuilder SetManufacturer(string manufacturer);

        IDeviceBuilder SetPowerStateSensor(DeviceValueGetter<bool> sensor);

        /// <summary>
        /// Sets an optional name to use when adding the device to a room (a name based on the type will be used by default, for example: 'Accessory').
        /// <para />
        /// Note: This does not apply to devices using discovery.
        /// </summary>
        IDeviceBuilder SetSpecificName(string? specificName);
    }

    internal sealed class DeviceBuilder : IDeviceBuilder
    {
        private readonly List<string> _additionalSearchTokens = new();
        private readonly List<ButtonDescriptor> _buttons = new();
        private readonly HashSet<DeviceCapability> _capabilities = new();

        public DeviceBuilder(string name)
        {
            Validator.ValidateStringLength(this.Name = name ?? throw new ArgumentNullException(nameof(name)), prefix: nameof(name));
            this.AdapterName = $"apt-{UniqueNameGenerator.Generate(name)}";
        }

        public string AdapterName { get; }

        public IReadOnlyCollection<string> AdditionalSearchTokens => this._additionalSearchTokens;

        public ButtonHandler? ButtonHandler { get; private set; }

        public IReadOnlyCollection<ButtonDescriptor> Buttons => this._buttons;

        public IReadOnlyCollection<DeviceCapability> Capabilities => this._capabilities;

        public DelaysSpecifier? Delays { get; private set; }

        public uint? DriverVersion { get; private set; }

        public IFavoritesHandler? FavoritesHandler { get; private set; }

        public DeviceIcon? Icon { get; private set; }

        public IDeviceInitializer? Initializer { get; private set; }

        public string Manufacturer { get; private set; } = "NEEO";

        public string Name { get; }

        public DeviceValueGetter<bool>? PowerStateSensor { get; private set; }

        IDeviceSetup IDeviceBuilder.Setup => this.Setup;

        public IDeviceSetup Setup { get; } = new DeviceSetup();

        public string? SpecificName { get; private set; }

        public DeviceType Type { get; init; }

        IDeviceBuilder IDeviceBuilder.AddAdditionalSearchToken(string text) => this.AddAdditionalSearchToken(text);

        IDeviceBuilder IDeviceBuilder.AddButton(ButtonDescriptor button) => this.AddButton(button);

        IDeviceBuilder IDeviceBuilder.AddButtonGroup(ButtonGroup group) => this.AddButtonGroup(group);

        IDeviceBuilder IDeviceBuilder.AddCapability(StaticDeviceCapability capability) => this.AddCapability(capability);

        IDeviceAdapter IDeviceBuilder.BuildAdapter() => this.BuildAdapter();

        IDeviceBuilder IDeviceBuilder.EnableDiscovery(
            DiscoveryOptions options,
            DiscoveryController controller
        ) => this.EnableDiscovery(options, controller);

        IDeviceBuilder IDeviceBuilder.SetButtonHandler(ButtonHandler handler) => this.SetButtonHandler(handler);

        IDeviceBuilder IDeviceBuilder.SetDelays(DelaysSpecifier delays) => this.SetDelays(delays);

        IDeviceBuilder IDeviceBuilder.SetDriverVersion(uint version) => this.SetDriverVersion(version);

        IDeviceBuilder IDeviceBuilder.SetFavoritesHandler(IFavoritesHandler handler) => this.SetFavoritesHandler(handler);

        IDeviceBuilder IDeviceBuilder.SetIcon(DeviceIcon icon) => this.SetIcon(icon);

        IDeviceBuilder IDeviceBuilder.SetInitializer(IDeviceInitializer initializer) => this.SetInitializer(initializer);

        IDeviceBuilder IDeviceBuilder.SetManufacturer(string manufacturer) => this.SetManufacturer(manufacturer);

        IDeviceBuilder IDeviceBuilder.SetPowerStateSensor(DeviceValueGetter<bool> sensor) => this.SetPowerStateSensor(sensor);

        IDeviceBuilder IDeviceBuilder.SetSpecificName(string? specificName) => this.SetSpecificName(specificName);

        private DeviceBuilder AddAdditionalSearchToken(string text)
        {
            this._additionalSearchTokens.Add(text);
            return this;
        }

        private DeviceBuilder AddButton(ButtonDescriptor button)
        {
            this._buttons.Add(button ?? throw new ArgumentNullException(nameof(button)));
            return this;
        }

        private DeviceBuilder AddButtonGroup(ButtonGroup group) => ButtonGroupAttribute.GetNames(group).Aggregate(
            this,
            static (builder, name) => builder.AddButton(name)
        );

        private IDeviceBuilder AddCapability(StaticDeviceCapability capability)
        {
            this._capabilities.Add((DeviceCapability)capability);
            return this;
        }

        private DeviceAdapter BuildAdapter()
        {
            if (this.Buttons.Count != 0 && this.ButtonHandler == null)
            {
                throw new InvalidOperationException();
            }
            if (this.Type.RequiresInput() && !this.Buttons.Any(button => button.Name.StartsWith(Constants.InputPrefix)))
            {
                throw new InvalidOperationException();
            }
            return new(
                this.AdapterName,
                this.Name,
                this.Type,
                this.Manufacturer,
                this.DriverVersion,
                this.Delays,
                this.AdditionalSearchTokens,
                this.SpecificName,
                this.Icon,
                this.Capabilities,
                this.Setup,
                this.Initializer
            );
        }

        private IDeviceBuilder EnableDiscovery(DiscoveryOptions options, DiscoveryController controller)
        {
            throw new NotImplementedException();
        }

        private DeviceBuilder SetButtonHandler(ButtonHandler handler)
        {
            this.ButtonHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            return this;
        }

        private DeviceBuilder SetDelays(DelaysSpecifier delays)
        {
            if (!this.Type.SupportsDelays())
            {
                throw new NotSupportedException($"Device type {this.Type} does not support delays.");
            }
            this.Delays = delays;
            return this;
        }

        private DeviceBuilder SetDriverVersion(uint version)
        {
            this.DriverVersion = version;
            return this;
        }

        private DeviceBuilder SetFavoritesHandler(IFavoritesHandler handler)
        {
            if (!this.Type.SupportsFavorites())
            {
                throw new NotSupportedException($"Device type {this.Type} does not support favorites.");
            }
            this.FavoritesHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            this._capabilities.Add(DeviceCapability.CustomFavoriteHandler);
            return this;
        }

        private DeviceBuilder SetIcon(DeviceIcon icon)
        {
            this.Icon = icon;
            return this;
        }

        private DeviceBuilder SetInitializer(IDeviceInitializer initializer)
        {
            this.Initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
            return this;
        }

        private DeviceBuilder SetManufacturer(string manufacturer = "NEEO")
        {
            Validator.ValidateStringLength(this.Manufacturer = manufacturer ?? throw new ArgumentNullException(nameof(manufacturer)));
            return this;
        }

        private DeviceBuilder SetPowerStateSensor(DeviceValueGetter<bool> sensor)
        {
            this.PowerStateSensor = sensor;
            return this;
        }

        private DeviceBuilder SetSpecificName(string? specificName)
        {
            if (specificName != null)
            {
                Validator.ValidateStringLength(specificName);
            }
            this.SpecificName = specificName;
            return this;
        }

        private static class Constants
        {
            public const string InputPrefix = "INPUT";
        }
    }
}
