using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Remote.Neeo.Devices.Descriptors;
using Remote.Neeo.Devices.Discovery;

namespace Remote.Neeo.Devices
{
    /// <summary>
    /// Fluent interface for building devices.
    /// </summary>
    public interface IDeviceBuilder
    {
        /// <summary>
        /// Gets the generated unique name for the device adapter.
        /// </summary>
        string AdapterName { get; }

        IReadOnlyCollection<string> AdditionalSearchTokens { get; }

        /// <summary>
        /// Gets the callback to be invoked in response to calls from the NEEO Brain to handle button presses.
        /// </summary>
        ButtonHandler? ButtonHandler { get; }

        IReadOnlyCollection<ButtonDescriptor> Buttons { get; }

        IReadOnlyCollection<DeviceCapability> Capabilities { get; }

        DiscoveryProcessor? DiscoveryProcessor { get; }

        /// <summary>
        /// Version of the device driver.
        /// Incrementing this version will cause the brain to query for new components.
        /// </summary>
        uint? DriverVersion { get; }

        FavoritesHandler? FavoritesHandler { get; }

        /// <summary>
        /// Gets the device icon override, if <c>null</c> a default is selected depending on the device type.
        /// </summary>
        DeviceIconOverride? Icon { get; }

        /// <summary>
        /// Device initializer callback.
        /// </summary>
        DeviceInitializer? Initializer { get; }

        /// <summary>
        /// Gets the device manufacturer name.
        /// <para/>
        /// If not set via <see cref="IDeviceBuilder.SetManufacturer"/>, the default value is &quot;NEEO&quot;.
        /// </summary>
        string Manufacturer { get; }

        string Name { get; }
        DeviceValueGetter<bool>? PowerStateSensor { get; }
        IDeviceSetup Setup { get; }
        string? SpecificName { get; }
        DeviceTiming? Timing { get; }

        /// <summary>
        /// The device type.
        /// </summary>
        DeviceType Type { get; }

        IDeviceBuilder AddAdditionalSearchToken(string token);

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

        IDeviceBuilder EnableRegistration(RegistrationOptions options, CredentialsProcessor processor, QueryIsRegistered queryIsRegistered);

        IDeviceBuilder EnableRegistration(RegistrationOptions options, SecurityCodeProcessor processor, QueryIsRegistered queryIsRegistered);

        /// <summary>
        /// Sets a callback to be invoked to initialize the device before making it available to the NEEO Brain.
        /// </summary>
        /// <param name="initializer">The device initializer callback.</param>
        /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
        IDeviceBuilder RegisterInitializer(DeviceInitializer initializer);

        /// <summary>
        /// Sets a callback to be invoked in response to calls from the NEEO Brain to handle button presses.
        /// </summary>
        /// <param name="handler">The button handler callback.</param>
        /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
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

        /// <summary>
        /// Sets a callback to be invoked in response to calls from the NEEO Brain to handle launching favorites.
        /// </summary>
        /// <param name="handler">The favorites handler callback.</param>
        /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
        IDeviceBuilder SetFavoritesHandler(FavoritesHandler handler);

        /// <summary>
        /// Sets the device icon override.
        /// <para/>
        /// The icon for a device is generally derived from the device type.
        /// NEEO supports two icon overrides (specifically &quot;sonos&quot; and &quot;neeo&quot;).
        /// </summary>
        /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
        IDeviceBuilder SetIcon(DeviceIconOverride icon);

        /// <summary>
        /// Sets the device manufacturer name (used in searching for devices).
        /// <para/>
        /// If not specified, the default of &quot;NEEO&quot; is used.
        /// </summary>
        /// <param name="manufacturer">Name of the device manufacturer.</param>
        /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
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
        private QueryIsRegistered? _queryIsRegistered;
        private Func<JsonElement, Task>? _registrationProcessor;

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

        public DiscoveryProcessor? DiscoveryProcessor { get; private set; }

        public uint? DriverVersion { get; private set; }

        public FavoritesHandler? FavoritesHandler { get; private set; }

        public DeviceIconOverride? Icon { get; private set; }

        public DeviceInitializer? Initializer { get; private set; }

        public string Manufacturer { get; private set; } = "NEEO";

        public string Name { get; }

        public DeviceValueGetter<bool>? PowerStateSensor { get; private set; }

        public Func<JsonElement, Task>? RegistrationProcessor { get; private set; }

        IDeviceSetup IDeviceBuilder.Setup => this.Setup;

        public DeviceSetup Setup { get; } = new DeviceSetup();

        public string? SpecificName { get; private set; }

        public DeviceTiming? Timing { get; private set; }

        public DeviceType Type { get; }

        IDeviceBuilder IDeviceBuilder.AddAdditionalSearchToken(string token) => this.AddAdditionalSearchToken(token);

        IDeviceBuilder IDeviceBuilder.AddButton(ButtonDescriptor button) => this.AddButton(button);

        IDeviceBuilder IDeviceBuilder.AddButtonGroup(ButtonGroup group) => this.AddButtons((KnownButtons)group);

        IDeviceBuilder IDeviceBuilder.AddButtons(KnownButtons button) => this.AddButtons(button);

        IDeviceBuilder IDeviceBuilder.AddCapability(DeviceCapability capability) => this.AddCapability(capability);

        IDeviceAdapter IDeviceBuilder.BuildAdapter() => this.BuildAdapter();

        IDeviceBuilder IDeviceBuilder.DefineTiming(DeviceTiming timing) => this.DefineTiming(timing);

        IDeviceBuilder IDeviceBuilder.EnableDiscovery(DiscoveryOptions options, DiscoveryProcessor processor) => this.EnableDiscovery(options, processor);

        IDeviceBuilder IDeviceBuilder.EnableRegistration(RegistrationOptions options, CredentialsProcessor processor, QueryIsRegistered queryIsRegistered) => this.EnableRegistration(
            options,
            RegistrationType.Credentials,
            processor == null
                ? throw new ArgumentNullException(nameof(processor))
                : element => processor(element.ToObject<Credentials>()),
            queryIsRegistered
        );

        IDeviceBuilder IDeviceBuilder.EnableRegistration(RegistrationOptions options, SecurityCodeProcessor processor, QueryIsRegistered queryIsRegistered) => this.EnableRegistration(
            options,
            RegistrationType.SecurityCode,
            processor == null
                ? throw new ArgumentNullException(nameof(processor))
                : element => processor(element.ToObject<SecurityCodeContainer>().SecurityCode),
            queryIsRegistered
        );

        IDeviceBuilder IDeviceBuilder.RegisterInitializer(DeviceInitializer initializer) => this.RegisterInitializer(initializer);

        IDeviceBuilder IDeviceBuilder.SetButtonHandler(ButtonHandler handler) => this.SetButtonHandler(handler);

        IDeviceBuilder IDeviceBuilder.SetDriverVersion(uint version) => this.SetDriverVersion(version);

        IDeviceBuilder IDeviceBuilder.SetFavoritesHandler(FavoritesHandler handler) => this.SetFavoritesHandler(handler);

        IDeviceBuilder IDeviceBuilder.SetIcon(DeviceIconOverride icon) => this.SetIcon(icon);

        IDeviceBuilder IDeviceBuilder.SetManufacturer(string manufacturer) => this.SetManufacturer(manufacturer);

        IDeviceBuilder IDeviceBuilder.SetPowerStateSensor(DeviceValueGetter<bool> sensor) => this.SetPowerStateSensor(sensor);

        IDeviceBuilder IDeviceBuilder.SetSpecificName(string? specificName) => this.SetSpecificName(specificName);

        private DeviceBuilder AddAdditionalSearchToken(string token)
        {
            this._additionalSearchTokens.Add(token);
            return this;
        }

        private DeviceBuilder AddButton(ButtonDescriptor button)
        {
            int index = this._buttons.BinarySearch(button ?? throw new ArgumentNullException(nameof(button)));
            if (index >= 0)
            {
                throw new ArgumentException($"Button \"{button.Name}\" already defined.", nameof(button));
            }
            this._buttons.Insert(~index, button);
            return this;
        }

        private DeviceBuilder AddButtons(KnownButtons buttons) => KnownButton.GetNames(buttons).Aggregate(
            this,
            static (builder, button) => builder.AddButton(button)
        );

        private DeviceBuilder AddCapability(DeviceCapability capability)
        {
            if (capability is not DeviceCapability.CustomFavoriteHandler and not DeviceCapability.RegisterUserAccount)
            {
                this._capabilities.Add(capability);
            }
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
                this.Timing,
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
            this.Timing = timing;
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

        private DeviceBuilder EnableRegistration(RegistrationOptions options, RegistrationType type, Func<JsonElement, Task> registrationProcessor, QueryIsRegistered queryIsRegistered)
        {
            if (!this.Setup.Discovery.GetValueOrDefault())
            {
                throw new InvalidOperationException();
            }
            if (this.Setup.RegistrationType.HasValue)
            {
                throw new InvalidOperationException();
            }
            if (options.Description == null) // Default constructor instance.
            {
                throw new ArgumentException($"Can not use uninitialized {nameof(options)}.", nameof(options));
            }
            this.Setup.RegistrationDescription = options.Description;
            this.Setup.RegistrationHeaderText = options.HeaderText;
            this.Setup.RegistrationType = type;
            this._registrationProcessor = registrationProcessor;
            this._queryIsRegistered = queryIsRegistered ?? throw new ArgumentNullException(nameof(queryIsRegistered));
            return this;
        }

        private DeviceBuilder RegisterInitializer(DeviceInitializer initializer)
        {
            this.Initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
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

        private DeviceBuilder SetManufacturer(string manufacturer = "NEEO")
        {
            Validator.ValidateStringLength(this.Manufacturer = manufacturer ?? throw new ArgumentNullException(nameof(manufacturer)));
            return this;
        }

        private DeviceBuilder SetPowerStateSensor(DeviceValueGetter<bool> sensor)
        {
            this.PowerStateSensor = sensor ?? throw new ArgumentNullException(nameof(sensor));
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