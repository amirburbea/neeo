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

        DeviceTiming? Delays { get; }

        DiscoveryProcessor? DiscoveryProcessor { get; }

        uint? DriverVersion { get; }

        FavoritesHandler? FavoritesHandler { get; }

        DeviceIconOverride? Icon { get; }

        DeviceInitializer? Initializer { get; }

        string Manufacturer { get; }

        string Name { get; }

        DeviceValueGetter<bool>? PowerStateSensor { get; }

        IDeviceSetup Setup { get; }

        string? SpecificName { get; }

        DeviceType Type { get; }

        IDeviceBuilder AddAdditionalSearchToken(string text);

        /// <summary>
        /// Add a button to the device.
        /// <para />
        /// Note that adding buttons to the device requires defining a button handler via <see cref="SetButtonHandler"/>.
        /// </summary>
        /// <param name="button">The button to add.</param>
        /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
        IDeviceBuilder AddButton(ButtonDescriptor button);

        /// <summary>
        /// Add a group of buttons to the device.
        /// <para />
        /// Note that adding buttons to the device requires defining a button handler via <see cref="SetButtonHandler"/>.
        /// </summary>
        /// <param name="group">The <see cref="ButtonGroup"/> to add.</param>
        /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
        IDeviceBuilder AddButtonGroup(ButtonGroup group);

        /// <summary>
        /// Add a button (or bitwise combination of buttons) to the device.
        /// <para />
        /// Note that adding buttons to the device requires defining a button handler via <see cref="SetButtonHandler"/>.
        /// </summary>
        /// <param name="buttons">The button (or bitwise combination of buttons) to add.</param>
        /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
        IDeviceBuilder AddButtons(KnownButtons buttons);

        IDeviceBuilder AddCapability(DeviceCapability capability);

        IDeviceAdapter BuildAdapter();

        /// <summary>
        /// Set timing related information (the delays NEEO should use when interacting with a device),
        /// which will be used when generating recipes.
        /// </summary>
        /// <param name="timing"><see cref="DeviceTiming"/> specifying delays NEEO should use when interacting with a device.</param>
        /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
        IDeviceBuilder DefineTiming(DeviceTiming timing);

        IDeviceBuilder EnableDiscovery(DiscoveryOptions options, DiscoveryProcessor controller);

        IDeviceBuilder SetButtonHandler(ButtonHandler handler);

        /// <summary>
        /// Setting the version allows you to tell the Brain about changes to your devices components. If you for example add new buttons to a device,
        /// you can increase the version and this will let the Brain know to fetch the new components.
        /// You do not need to update the version if you do not change the components. When adding a version to a device that was previously not versioned,
        /// start with 1. The Brain will assume it was previously 0 and update.
        /// <para />
        /// Note: The Brain will only add new components, updating or removing old components is not supported)
        /// </summary>
        /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
        IDeviceBuilder SetDriverVersion(uint version);

        IDeviceBuilder SetFavoritesHandler(FavoritesHandler handler);

        IDeviceBuilder SetIcon(DeviceIconOverride icon);

        IDeviceBuilder SetInitializer(DeviceInitializer initializer);

        IDeviceBuilder SetManufacturer(string manufacturer);

        IDeviceBuilder SetPowerStateSensor(DeviceValueGetter<bool> sensor);

        /// <summary>
        /// Sets an optional name to use when adding the device to a room (a name based on the type will be used by default, for example: 'Accessory').
        /// <para />
        /// Note: This does not apply to devices using discovery.
        /// </summary>
        /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
        IDeviceBuilder SetSpecificName(string? specificName);
    }

    internal sealed class DeviceBuilder : IDeviceBuilder
    {
        private readonly List<string> _additionalSearchTokens = new();
        private readonly List<ButtonDescriptor> _buttons = new();
        private readonly HashSet<DeviceCapability> _capabilities = new();

        public DeviceBuilder(string name, DeviceType type, string? prefix)
        {
            this.Type = type;
            Validator.ValidateStringLength(this.Name = name ?? throw new ArgumentNullException(nameof(name)), prefix: nameof(name));
            this.AdapterName = $"apt-{UniqueNameGenerator.Generate(name, prefix)}";
        }

        public string AdapterName { get; }

        public IReadOnlyCollection<string> AdditionalSearchTokens => this._additionalSearchTokens;

        public ButtonHandler? ButtonHandler { get; private set; }

        public IReadOnlyCollection<ButtonDescriptor> Buttons => this._buttons;

        public IReadOnlyCollection<DeviceCapability> Capabilities => this._capabilities;

        public DeviceTiming? Delays { get; private set; }

        public DiscoveryProcessor? DiscoveryProcessor { get; private set; }

        public uint? DriverVersion { get; private set; }

        public FavoritesHandler? FavoritesHandler { get; private set; }

        public DeviceIconOverride? Icon { get; private set; }

        public DeviceInitializer? Initializer { get; private set; }

        public string Manufacturer { get; private set; } = "NEEO";

        public string Name { get; }

        public DeviceValueGetter<bool>? PowerStateSensor { get; private set; }

        IDeviceSetup IDeviceBuilder.Setup => this.Setup;

        public DeviceSetup Setup { get; } = new DeviceSetup();

        public string? SpecificName { get; private set; }

        public DeviceType Type { get; }

        IDeviceBuilder IDeviceBuilder.AddAdditionalSearchToken(string text) => this.AddAdditionalSearchToken(text);

        IDeviceBuilder IDeviceBuilder.AddButton(ButtonDescriptor button) => this.AddButton(button);

        IDeviceBuilder IDeviceBuilder.AddButtonGroup(ButtonGroup group) => this.AddButtons((KnownButtons)group);

        IDeviceBuilder IDeviceBuilder.AddButtons(KnownButtons button) => this.AddButtons(button);

        IDeviceBuilder IDeviceBuilder.AddCapability(DeviceCapability capability) => this.AddCapability(capability);

        IDeviceAdapter IDeviceBuilder.BuildAdapter() => this.BuildAdapter();

        IDeviceBuilder IDeviceBuilder.DefineTiming(DeviceTiming timing) => this.DefineTiming(timing);

        IDeviceBuilder IDeviceBuilder.EnableDiscovery(DiscoveryOptions options, DiscoveryProcessor processor) => this.EnableDiscovery(options, processor);

        IDeviceBuilder IDeviceBuilder.SetButtonHandler(ButtonHandler handler) => this.SetButtonHandler(handler);

        IDeviceBuilder IDeviceBuilder.SetDriverVersion(uint version) => this.SetDriverVersion(version);

        IDeviceBuilder IDeviceBuilder.SetFavoritesHandler(FavoritesHandler handler) => this.SetFavoritesHandler(handler);

        IDeviceBuilder IDeviceBuilder.SetIcon(DeviceIconOverride icon) => this.SetIcon(icon);

        IDeviceBuilder IDeviceBuilder.SetInitializer(DeviceInitializer initializer) => this.SetInitializer(initializer);

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
            if (button == null)
            {
                throw new ArgumentNullException(nameof(button));
            }
            if (this.Buttons.Any(existing => existing.Name == button.Name))
            {
                throw new ArgumentException($"Button \"{button.Name}\" already defined.", nameof(button));
            }
            this._buttons.Add(button ?? throw new ArgumentNullException(nameof(button)));
            return this;
        }

        private DeviceBuilder AddButtons(KnownButtons buttons) => KnownButton.GetNames(buttons).Aggregate(
            this,
            static (builder, button) => builder.AddButton(button)
        );

        private DeviceBuilder AddCapability(DeviceCapability capability)
        {
            if (capability == DeviceCapability.CustomFavoriteHandler && this.FavoritesHandler == null)
            {
                throw new ArgumentException($"Can not add the capability {capability} before calling {nameof(IDeviceBuilder.SetFavoritesHandler)}.", nameof(capability));
            }
            if (capability == DeviceCapability.RegisterUserAccount && !this.Setup.Registration.GetValueOrDefault())
            {
                throw new ArgumentException("", nameof(capability));
            }
            this._capabilities.Add(capability);
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

        private DeviceBuilder DefineTiming(DeviceTiming timing)
        {
            if (!this.Type.SupportsTiming())
            {
                throw new NotSupportedException($"Device type {this.Type} does not support timing.");
            }
            this.Delays = timing;
            return this;
        }

        private DeviceBuilder EnableDiscovery(DiscoveryOptions options, DiscoveryProcessor processor)
        {
            this.DiscoveryProcessor = processor ?? throw new ArgumentNullException(nameof(processor));
            this.Setup.Discovery = true;
            this.Setup.HeaderText = options.HeaderText;
            this.Setup.Description = options.Description;
            this.Setup.EnableDynamicDeviceBuilder = options.EnableDynamicDeviceBuilder;
            return this;
        }

        private DeviceBuilder SetButtonHandler(ButtonHandler handler)
        {
            this.ButtonHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            return this;
        }

        private DeviceBuilder SetDriverVersion(uint version)
        {
            this.DriverVersion = version;
            return this;
        }

        private DeviceBuilder SetFavoritesHandler(FavoritesHandler handler)
        {
            if (!this.Type.SupportsFavorites())
            {
                throw new NotSupportedException($"Device type {this.Type} does not support favorites.");
            }
            this.FavoritesHandler = handler ?? throw new ArgumentNullException(nameof(handler));
            return this.AddCapability(DeviceCapability.CustomFavoriteHandler);
        }

        private DeviceBuilder SetIcon(DeviceIconOverride icon)
        {
            this.Icon = icon;
            return this;
        }

        private DeviceBuilder SetInitializer(DeviceInitializer initializer)
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
