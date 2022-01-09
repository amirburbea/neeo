using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Remote.Neeo.Devices.Components;
using Remote.Neeo.Devices.Discovery;
using Remote.Neeo.Utilities;

namespace Remote.Neeo.Devices;

/// <summary>
/// Fluent interface for building devices.
/// </summary>
public interface IDeviceBuilder
{
    /// <summary>
    /// Gets the generated unique name for the device adapter.
    /// </summary>
    string AdapterName { get; }

    /// <summary>
    /// Gets the collection of additional search tokens defined for the device.
    /// </summary>
    IReadOnlyCollection<string> AdditionalSearchTokens { get; }

    /// <summary>
    /// Gets the callback to be invoked in response to calls from the NEEO Brain to handle button presses.
    /// </summary>
    ButtonHandler? ButtonHandler { get; }

    /// <summary>
    /// Gets the collection of buttons defined.
    /// </summary>
    IReadOnlyCollection<DeviceFeature> Buttons { get; }

    /// <summary>
    /// Gets the collection of special characteristics of the device.
    /// </summary>
    IReadOnlyCollection<Characteristic> Characteristics { get; }

    /// <summary>
    /// Gets a set of callbacks which allow tracking which devices are currently in use on the NEEO Brain.
    /// This can be used to avoid sending Brain notifications for devices not added on the Brain, to free up
    /// resource and/or remove credentials when the last device is removed.
    /// </summary>
    DeviceSubscriptionCallbacks? DeviceSubscriptionHandlers { get; }

    DiscoveryProcessor? DiscoveryProcessor { get; }

    /// <summary>
    /// Version of the device driver.
    /// Incrementing this version will cause the brain to query for new components.
    /// </summary>
    uint? DriverVersion { get; }

    FavoritesHandler? FavoritesHandler { get; }

    /// <summary>
    /// Gets a value indicating if a power state sensor has been defined for the device.
    /// </summary>
    bool HasPowerStateSensor { get; }

    /// <summary>
    /// Gets the device icon override, if <c>null</c> a default is selected depending on the device type.
    /// </summary>
    DeviceIconOverride? Icon { get; }

    /// <summary>
    /// Gets the collection of ImageUrl features defined for the device.
    /// </summary>
    IReadOnlyCollection<DeviceFeature> ImageUrls { get; }

    /// <summary>
    /// Device initializer callback.
    /// </summary>
    DeviceInitializer? Initializer { get; }

    /// <summary>
    /// Gets the device manufacturer name.
    /// </summary>
    /// <remarks>If not set via <see cref="SetManufacturer"/>, the default value is &quot;NEEO&quot;.</remarks>
    string Manufacturer { get; }

    /// <summary>
    /// Gets the name of the device.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a function used by the Brain to determine if registration is not required.
    /// </summary>
    QueryIsRegistered? QueryIsRegistered { get; }

    /// <summary>
    /// Gets a function used by the Brain to attempt registration of the device.
    /// </summary>
    Func<JsonElement, Task>? RegistrationProcessor { get; }

    /// <summary>
    /// Gets the collection of sensors defined for the device.
    /// </summary>
    IReadOnlyCollection<DeviceFeature> Sensors { get; }

    IDeviceSetup Setup { get; }

    /// <summary>
    /// Gets the collection of sliders defined for the device.
    /// </summary>
    IReadOnlyCollection<DeviceFeature> Sliders { get; }

    /// <summary>
    /// Gets an optional name to use when adding the device to a room
    /// (a name based on the type will be used by default, for example: 'Accessory').
    /// </summary>
    /// <remarks>Note: This does not apply to devices using discovery.</remarks>
    string? SpecificName { get; }

    SubscriptionFunction? SubscriptionFunction { get; }

    /// <summary>
    /// Gets the collection of switches defined for the device.
    /// </summary>
    IReadOnlyCollection<DeviceFeature> Switches { get; }

    /// <summary>
    /// Gets the collection of text labels defined for the device.
    /// </summary>
    IReadOnlyCollection<DeviceFeature> TextLabels { get; }

    /// <summary>
    /// Get timing related information (the delays NEEO should use when interacting with a device),
    /// which will be used when generating recipes.
    /// </summary>
    DeviceTiming? Timing { get; }

    /// <summary>
    /// The device type.
    /// </summary>
    DeviceType Type { get; }

    IDeviceBuilder AddAdditionalSearchToken(string token);

    /// <summary>
    /// Add a button to the device.
    /// </summary>
    /// <param name="name">The name of the button to add.</param>
    /// <param name="label">Optional - the label to use in place of the name.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    /// <remarks>Note that adding buttons to the device requires defining a button handler via
    /// <see cref="AddButtonHandler"/>.</remarks>
    IDeviceBuilder AddButton(string name, string? label = default);

    /// <summary>
    /// Add a group of buttons to the device.
    /// </summary>
    /// <param name="group">The <see cref="ButtonGroup"/> to add.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    /// <remarks>Note that adding buttons to the device requires defining a button handler via
    /// <see cref="AddButtonHandler"/>.</remarks>
    IDeviceBuilder AddButtonGroup(ButtonGroup group);

    /// <summary>
    /// Sets a callback to be invoked in response to calls from the NEEO Brain to handle button presses.
    /// </summary>
    /// <param name="handler">The button handler callback.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    IDeviceBuilder AddButtonHandler(ButtonHandler handler);

    /// <summary>
    /// Add a button (or bitwise combination of buttons) to the device.
    /// </summary>
    /// <param name="buttons">The button (or bitwise combination of buttons) to add.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    /// <remarks>Note that adding buttons to the device requires defining a button handler via
    /// <see cref="AddButtonHandler"/>.</remarks>
    IDeviceBuilder AddButtons(KnownButtons buttons);

    IDeviceBuilder AddCharacteristic(Characteristic characteristic);

    IDeviceBuilder AddImageUrl(string name, string? label, ImageSize size, DeviceValueGetter<string> getter);

    IDeviceBuilder AddPowerStateSensor(DeviceValueGetter<bool> sensor);

    IDeviceBuilder AddSensor(string name, string? label, double rangeLow, double rangeHigh, string units, DeviceValueGetter<double> getter);

    IDeviceBuilder AddSlider(string name, string? label, double rangeLow, double rangeHigh, string units, DeviceValueGetter<double> getter, DeviceValueSetter<double> setter);

    IDeviceBuilder AddSwitch(string name, string? label, DeviceValueGetter<bool> getter, DeviceValueSetter<bool> setter);

    IDeviceBuilder AddTextLabel(string name, string? label, bool? isLabelVisible, DeviceValueGetter<string> getter);

    IDeviceAdapter BuildAdapter(string sdkAdapterName);

    /// <summary>
    /// Set timing related information (the delays NEEO should use when interacting with a device),
    /// which will be used when generating recipes.
    /// </summary>
    /// <param name="timing"><see cref="DeviceTiming"/> specifying delays NEEO should use when interacting with
    /// a device.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    IDeviceBuilder DefineTiming(DeviceTiming timing);

    IDeviceBuilder EnableDiscovery(DiscoveryOptions options, DiscoveryProcessor controller);

    IDeviceBuilder EnableRegistration(RegistrationOptions options, RegistrationCallbacks callbacks);

    /// <summary>
    /// Specify a set of callbacks which allow tracking which devices are currently in use on the NEEO Brain.
    /// This can be used to avoid sending Brain notifications for devices not added on the Brain, to free up
    /// resource and/or remove credentials when the last device is removed.
    /// </summary>
    /// <param name="handlers">The device subscription handlers.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    IDeviceBuilder RegisterDeviceSubscriptionHandlers(DeviceSubscriptionCallbacks handlers);

    /// <summary>
    /// Sets a callback to be invoked in response to calls from the NEEO Brain to handle launching favorites.
    /// </summary>
    /// <param name="handler">The favorites handler callback.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    IDeviceBuilder RegisterFavoritesHandler(FavoritesHandler handler);

    /// <summary>
    /// Sets a callback to be invoked to initialize the device before making it available to the NEEO Brain.
    /// </summary>
    /// <param name="initializer">The device initializer callback.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    IDeviceBuilder RegisterInitializer(DeviceInitializer initializer);

    IDeviceBuilder RegisterSubscriptionFunction(SubscriptionFunction function);

    /// <summary>
    /// Setting the version allows you to tell the Brain about changes to your devices components.
    /// If you for example add new buttons to a device, you can increase the version and this will let the Brain
    /// know to fetch the new components. You do not need to update the version if you do not change the components.
    /// When adding a version to a device that was previously not versioned, start with 1. The Brain will assume it
    /// was previously 0 and update.
    /// </summary>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    /// <remarks>Note: The Brain will only add components, updating/removing components is not supported.</remarks>
    IDeviceBuilder SetDriverVersion(uint version);

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
    /// </summary>
    /// <param name="manufacturer">Name of the device manufacturer.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    /// <remarks>If not specified, the default of &quot;NEEO&quot; is used.</remarks>
    IDeviceBuilder SetManufacturer(string manufacturer);

    /// <summary>
    /// Sets an optional name to use when adding the device to a room
    /// (a name based on the type will be used by default, for example: 'Accessory').
    /// </summary>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    /// <remarks>Note: This does not apply to devices using discovery.</remarks>
    IDeviceBuilder SetSpecificName(string? specificName);
}

internal sealed class DeviceBuilder : IDeviceBuilder
{
    private static readonly Comparer<DeviceFeature> _featureComparer = ProjectionComparer.Create(
        (DeviceFeature feature) => feature.Name, 
        StringComparer.OrdinalIgnoreCase
    );

    private readonly List<string> _additionalSearchTokens = new();
    private readonly List<DeviceFeature> _buttons = new();
    private readonly HashSet<Characteristic> _deviceCapabilities = new();
    private readonly List<DeviceFeature> _imageUrls = new();
    private readonly List<DeviceFeature> _sensors = new();
    private readonly List<DeviceFeature> _sliders = new();
    private readonly List<DeviceFeature> _switches = new();
    private readonly List<DeviceFeature> _textLabels = new();

    internal DeviceBuilder(string name, DeviceType type, string? prefix)
    {
        this.Type = type;
        Validator.ValidateString(this.Name = name, name: nameof(name));
        this.AdapterName = $"apt-{UniqueNameGenerator.Generate(name, prefix)}";
    }

    public string AdapterName { get; }

    public IReadOnlyCollection<string> AdditionalSearchTokens => this._additionalSearchTokens;

    public ButtonHandler? ButtonHandler { get; private set; }

    public IReadOnlyCollection<DeviceFeature> Buttons => this._buttons;

    public IReadOnlyCollection<Characteristic> Characteristics => this._deviceCapabilities;

    public DeviceSubscriptionCallbacks? DeviceSubscriptionHandlers { get; private set; }

    public DiscoveryProcessor? DiscoveryProcessor { get; private set; }

    public uint? DriverVersion { get; private set; }

    public FavoritesHandler? FavoritesHandler { get; private set; }

    public bool HasPowerStateSensor => this._sensors.Any(sensor => sensor.Type == ComponentType.Power);

    public DeviceIconOverride? Icon { get; private set; }

    public IReadOnlyCollection<DeviceFeature> ImageUrls => this._imageUrls;

    public DeviceInitializer? Initializer { get; private set; }

    public string Manufacturer { get; private set; } = "NEEO";

    public string Name { get; }

    public QueryIsRegistered? QueryIsRegistered { get; private set; }

    public Func<JsonElement, Task>? RegistrationProcessor { get; private set; }

    public IReadOnlyCollection<DeviceFeature> Sensors => this._sensors;

    IDeviceSetup IDeviceBuilder.Setup => this.Setup;

    public DeviceSetup Setup { get; } = new DeviceSetup();

    public IReadOnlyCollection<DeviceFeature> Sliders => this._sliders;

    public string? SpecificName { get; private set; }

    public SubscriptionFunction? SubscriptionFunction { get; private set; }

    public IReadOnlyCollection<DeviceFeature> Switches => this._switches;

    public IReadOnlyCollection<DeviceFeature> TextLabels => this._textLabels;

    public DeviceTiming? Timing { get; private set; }

    public DeviceType Type { get; }

    IDeviceBuilder IDeviceBuilder.AddAdditionalSearchToken(string token)
    {
        return this.AddAdditionalSearchToken(token);
    }

    IDeviceBuilder IDeviceBuilder.AddButton(string name, string? label)
    {
        return this.AddButton(name, label);
    }

    IDeviceBuilder IDeviceBuilder.AddButtonGroup(ButtonGroup group)
    {
        return this.AddButtons((KnownButtons)group);
    }

    IDeviceBuilder IDeviceBuilder.AddButtonHandler(ButtonHandler handler)
    {
        return this.AddButtonHandler(handler);
    }

    IDeviceBuilder IDeviceBuilder.AddButtons(KnownButtons button)
    {
        return this.AddButtons(button);
    }

    IDeviceBuilder IDeviceBuilder.AddCharacteristic(Characteristic characteristic)
    {
        return this.AddCharacteristic(characteristic);
    }

    IDeviceBuilder IDeviceBuilder.AddImageUrl(string name, string? label, ImageSize size, DeviceValueGetter<string> getter)
    {
        return this.AddImageUrl(name, label, size, getter);
    }

    IDeviceBuilder IDeviceBuilder.AddPowerStateSensor(DeviceValueGetter<bool> sensor)
    {
        return this.AddPowerStateSensor(sensor);
    }

    IDeviceBuilder IDeviceBuilder.AddSensor(string name, string? label, double rangeLow, double rangeHigh, string units, DeviceValueGetter<double> getter)
    {
        return this.AddSensor(name, label, rangeLow, rangeHigh, units, getter);
    }

    IDeviceBuilder IDeviceBuilder.AddSlider(string name, string? label, double rangeLow, double rangeHigh, string units, DeviceValueGetter<double> getter, DeviceValueSetter<double> setter)
    {
        return this.AddSlider(name, label, rangeLow, rangeHigh, units, getter, setter);
    }

    IDeviceBuilder IDeviceBuilder.AddSwitch(string name, string? label, DeviceValueGetter<bool> getter, DeviceValueSetter<bool> setter)
    {
        return this.AddSwitch(name, label, getter, setter);
    }

    IDeviceBuilder IDeviceBuilder.AddTextLabel(string name, string? label, bool? isLabelVisible, DeviceValueGetter<string> getter)
    {
        return this.AddTextLabel(name, label, isLabelVisible, getter);
    }

    IDeviceAdapter IDeviceBuilder.BuildAdapter(string sdkAdapterName)
    {
        return this.BuildAdapter(sdkAdapterName);
    }

    IDeviceBuilder IDeviceBuilder.DefineTiming(DeviceTiming timing)
    {
        return this.DefineTiming(timing);
    }

    IDeviceBuilder IDeviceBuilder.EnableDiscovery(DiscoveryOptions options, DiscoveryProcessor processor)
    {
        return this.EnableDiscovery(options, processor);
    }

    IDeviceBuilder IDeviceBuilder.EnableRegistration(RegistrationOptions options, RegistrationCallbacks callbacks)
    {
        return this.EnableRegistration(options, callbacks);
    }

    IDeviceBuilder IDeviceBuilder.RegisterDeviceSubscriptionHandlers(DeviceSubscriptionCallbacks handlers)
    {
        return this.RegisterDeviceSubscriptionHandlers(handlers);
    }

    IDeviceBuilder IDeviceBuilder.RegisterFavoritesHandler(FavoritesHandler handler)
    {
        return this.RegisterFavoritesHandler(handler);
    }

    IDeviceBuilder IDeviceBuilder.RegisterInitializer(DeviceInitializer initializer)
    {
        return this.RegisterInitializer(initializer);
    }

    IDeviceBuilder IDeviceBuilder.RegisterSubscriptionFunction(SubscriptionFunction function)
    {
        return this.RegisterSubscriptionFunction(function);
    }

    IDeviceBuilder IDeviceBuilder.SetDriverVersion(uint version)
    {
        return this.SetDriverVersion(version);
    }

    IDeviceBuilder IDeviceBuilder.SetIcon(DeviceIconOverride icon)
    {
        return this.SetIcon(icon);
    }

    IDeviceBuilder IDeviceBuilder.SetManufacturer(string manufacturer)
    {
        return this.SetManufacturer(manufacturer);
    }

    IDeviceBuilder IDeviceBuilder.SetSpecificName(string? specificName)
    {
        return this.SetSpecificName(specificName);
    }

    private DeviceBuilder AddAdditionalSearchToken(string token)
    {
        this._additionalSearchTokens.Add(token);
        return this;
    }

    private DeviceBuilder AddButton(string name, string? label = default)
    {
        DeviceFeature feature = new(ComponentType.Button, name, label);
        int index = this._buttons.BinarySearch(feature, DeviceBuilder._featureComparer);
        if (index >= 0)
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        this._buttons.Insert(~index, feature);
        return this;
    }

    private DeviceBuilder AddButtonHandler(ButtonHandler handler)
    {
        if (this.ButtonHandler != null)
        {
            throw new InvalidOperationException("ButtonHandler already defined.");
        }
        this.ButtonHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        return this;
    }

    private DeviceBuilder AddButtons(KnownButtons buttons) => KnownButton.GetNames(buttons).Aggregate(
        this,
        static (builder, name) => builder.AddButton(name)
    );

    private DeviceBuilder AddCharacteristic(Characteristic capability)
    {
        if (capability is Characteristic.CustomFavoriteHandler or Characteristic.RegisterUserAccount)
        {
            throw new InvalidOperationException("ButtonHandler already defined.");
        }
        this._deviceCapabilities.Add(capability);
        return this;
    }

    private DeviceBuilder AddImageUrl(string name, string? label, ImageSize size, DeviceValueGetter<string> getter)
    {
        this._imageUrls.Add(new(ComponentType.ImageUrl, name, label, size: size)
        {
            Controller = ComponentController.Create(getter)
        });
        return this;
    }

    private DeviceBuilder AddPowerStateSensor(DeviceValueGetter<bool> getter)
    {
        if (this.HasPowerStateSensor)
        {
            throw new InvalidOperationException("PowerStateSensor already added.");
        }
        this._sensors.Add(new(ComponentType.Power, Constants.PowerSensorName, Constants.PowerSensorLabel)
        {
            Controller = ComponentController.Create(getter)
        });
        return this;
    }

    private DeviceBuilder AddSensor(string name, string? label, double rangeLow, double rangeHigh, string units, DeviceValueGetter<double> getter)
    {
        if (name == Constants.PowerSensorName)
        {
            throw new ArgumentException($"Name can not be {Constants.PowerSensorName}.", nameof(name));
        }
        this._sensors.Add(new(ComponentType.Sensor, name, label, rangeLow: rangeLow, rangeHigh: rangeHigh, unit: units)
        {
            Controller = ComponentController.Create(getter)
        });
        return this;
    }

    private DeviceBuilder AddSlider(string name, string? label, double rangeLow, double rangeHigh, string units, DeviceValueGetter<double> getter, DeviceValueSetter<double> setter)
    {
        this._sliders.Add(new(ComponentType.Slider, name, label, rangeLow: rangeLow, rangeHigh: rangeHigh, unit: units)
        {
            Controller = ComponentController.Create(getter, setter ?? throw new ArgumentNullException(nameof(setter)))
        });
        return this;
    }

    private DeviceBuilder AddSwitch(string name, string? label, DeviceValueGetter<bool> getter, DeviceValueSetter<bool> setter)
    {
        this._switches.Add(new(ComponentType.Switch, name, label)
        {
            Controller = ComponentController.Create(getter, setter ?? throw new ArgumentNullException(nameof(setter)))
        });
        return this;
    }

    private DeviceBuilder AddTextLabel(string name, string? label, bool? isLabelVisible, DeviceValueGetter<string> getter)
    {
        this._textLabels.Add(new(ComponentType.TextLabel, name, label)
        {
            Controller = ComponentController.Create(getter)
        });
        return this;
    }

    private bool HasInputButton() => this.Buttons.Any(button => button.Name.StartsWith(Constants.InputPrefix));

    private DeviceAdapter BuildAdapter(string sdkAdapterName)
    {
        if (this.Type.RequiresInput() && !this.HasInputButton())
        {
            throw new InvalidOperationException();
        }
        if (this.ButtonHandler is { } handler)
        {
            this._buttons.ForEach(button => button.Controller = ComponentController.Create(async deviceId =>
            {
                await handler(deviceId, button.Name).ConfigureAwait(false);
                return true;
            }));
        }
        else if (this.Buttons.Count != 0)
        {
            throw new InvalidOperationException("There are buttons defined but no handler was specified.");
        }
        (IReadOnlyDictionary<string, ICapabilityHandler> handlers, IReadOnlyCollection<IComponent> capabilities) = CapabilityHandler.Build(this);
        return new(
            this.AdapterName,
            this.Type,
            this.Manufacturer,
            this.DriverVersion,
            this.Timing,
            this.Characteristics,
            this.Setup,
            this.Initializer,
            this.Name,
            this.AdditionalSearchTokens,
            this.SpecificName,
            this.Icon,
            capabilities,
            handlers
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

    private DeviceBuilder EnableRegistration(RegistrationOptions options, RegistrationCallbacks controller)
    {
        if (!this.Setup.Discovery.GetValueOrDefault())
        {
            throw new InvalidOperationException("Registration is only supported on devices with discovery. " +
                $"(Call {nameof(this.EnableDiscovery)} first).");
        }
        if (this.Setup.RegistrationType.HasValue)
        {
            throw new InvalidOperationException("Registration is already defined.");
        }
        if (options.Description == null) // Default constructor instance.
        {
            throw new ArgumentException($"Can not use uninitialized {nameof(options)}.", nameof(options));
        }
        this.Setup.RegistrationDescription = options.Description;
        this.Setup.RegistrationHeaderText = options.HeaderText;
        this.Setup.RegistrationType = controller.RegistrationType;
        this.RegistrationProcessor = controller.Processor;
        this.QueryIsRegistered = controller.QueryIsRegistered;
        return this;
    }

    private DeviceBuilder RegisterDeviceSubscriptionHandlers(DeviceSubscriptionCallbacks handlers)
    {
        if (this.DeviceSubscriptionHandlers != null)
        {
            throw new InvalidOperationException($"{nameof(this.DeviceSubscriptionHandlers)} already defined.");
        }
        this.DeviceSubscriptionHandlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
        return this;
    }

    private DeviceBuilder RegisterFavoritesHandler(FavoritesHandler handler)
    {
        if (this.FavoritesHandler != null)
        {
            throw new InvalidOperationException($"{nameof(FavoritesHandler)} already defined.");
        }
        if (!this.Type.SupportsFavorites())
        {
            throw new NotSupportedException($"Device type {this.Type} does not support favorites.");
        }
        this.FavoritesHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        return this.AddCharacteristic(Characteristic.CustomFavoriteHandler);
    }

    private DeviceBuilder RegisterInitializer(DeviceInitializer initializer)
    {
        if (this.Initializer != null)
        {
            throw new InvalidOperationException($"{nameof(DeviceInitializer)} already defined.");
        }
        this.Initializer = initializer ?? throw new ArgumentNullException(nameof(initializer));
        return this;
    }

    private DeviceBuilder RegisterSubscriptionFunction(SubscriptionFunction function)
    {
        if (this.SubscriptionFunction != null)
        {
            throw new InvalidOperationException($"{nameof(SubscriptionFunction)} already defined.");
        }
        this.SubscriptionFunction = function;
        return this;
    }

    private DeviceBuilder SetDriverVersion(uint version)
    {
        this.DriverVersion = version;
        return this;
    }

    private DeviceBuilder SetIcon(DeviceIconOverride icon)
    {
        this.Icon = icon;
        return this;
    }

    private DeviceBuilder SetManufacturer(string manufacturer = "NEEO")
    {
        Validator.ValidateString(this.Manufacturer = manufacturer);
        return this;
    }

    private DeviceBuilder SetSpecificName(string? specificName)
    {
        Validator.ValidateString(this.SpecificName = specificName, allowNull: true);
        return this;
    }

    private static class Constants
    {
        public const string InputPrefix = "INPUT";
        public const string PowerSensorName = "powerstate";
        public const string PowerSensorLabel = "Powerstate";
    }
}
