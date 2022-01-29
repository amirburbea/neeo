using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Components;
using Neeo.Sdk.Devices.Controllers;
using Neeo.Sdk.Devices.Discovery;
using Neeo.Sdk.Json;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

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
    IReadOnlyCollection<string> Buttons { get; }

    /// <summary>
    /// Gets the collection of special characteristics of the device.
    /// </summary>
    IReadOnlyCollection<DeviceCharacteristic> Characteristics { get; }

    IDiscoveryFeature? DiscoveryController { get; }

    /// <summary>
    /// Version of the device driver.
    /// Incrementing this version will cause the brain to query for new components.
    /// </summary>
    uint? DriverVersion { get; }

    IFavoritesController? FavoritesController { get; }

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
    IReadOnlyCollection<string> ImageUrls { get; }

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

    DeviceNotifierCallback? NotifierCallback { get; }

    IRegistrationFeature? RegistrationController { get; }

    /// <summary>
    /// Gets the name of the sensors defined for the device.
    /// </summary>
    IReadOnlyCollection<string> Sensors { get; }

    IDeviceSetup Setup { get; }

    /// <summary>
    /// Gets the collection of sliders defined for the device.
    /// </summary>
    IReadOnlyCollection<string> Sliders { get; }

    /// <summary>
    /// Gets an optional name to use when adding the device to a room
    /// (a name based on the type will be used by default, for example: 'Accessory').
    /// </summary>
    /// <remarks>Note: This does not apply to devices using discovery.</remarks>
    string? SpecificName { get; }

    /// <summary>
    /// Gets a set of callbacks which allow tracking which devices are currently in use on the NEEO Brain.
    /// This can be used to avoid sending Brain notifications for devices not added on the Brain, to free up
    /// resource and/or remove credentials when the last device is removed.
    /// </summary>
    ISubscriptionController? SubscriptionController { get; }

    /// <summary>
    /// Gets the collection of switches defined for the device.
    /// </summary>
    IReadOnlyCollection<string> Switches { get; }

    /// <summary>
    /// Gets the collection of text labels defined for the device.
    /// </summary>
    IReadOnlyCollection<string> TextLabels { get; }

    /// <summary>
    /// Get timing related information (the delays NEEO should use when interacting with a device),
    /// which will be used when generating recipes.
    /// </summary>
    DeviceTiming? Timing { get; }

    /// <summary>
    /// The device type.
    /// </summary>
    DeviceType Type { get; }

    IDeviceBuilder AddAdditionalSearchTokens(params string[] tokens);

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
    /// <param name="group">The <see cref="ButtonGroups"/> to add.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    /// <remarks>Note that adding buttons to the device requires defining a button handler via
    /// <see cref="AddButtonHandler"/>.</remarks>
    IDeviceBuilder AddButtonGroups(ButtonGroups group) => this.AddButtons((KnownButtons)group);

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

    IDeviceBuilder AddCharacteristic(DeviceCharacteristic characteristic);

    IDeviceBuilder AddImageUrl(string name, string? label, ImageSize size, string? uri = default, DeviceValueGetter<string>? getter = default);

    IDeviceBuilder AddPowerStateSensor(DeviceValueGetter<bool> sensor);

    IDeviceBuilder AddSensor(
        string name,
        string? label,
        DeviceValueGetter<double> getter,
        double rangeLow = 0d,
        double rangeHigh = 100d,
        string unit = "%"
    );

    IDeviceBuilder AddSensor(
        string name,
        string? label,
        DeviceValueGetter<bool> getter
    );

    IDeviceBuilder AddSensor(
        string name,
        string? label,
        DeviceValueGetter<string> getter
    );

    IDeviceBuilder AddSlider(
        string name,
        string? label,
        DeviceValueGetter<double> getter,
        DeviceValueSetter<double> setter,
        double rangeLow = 0d,
        double rangeHigh = 100d,
        string unit = "%"
    );

    IDeviceBuilder AddSwitch(string name, string? label, DeviceValueGetter<bool> getter, DeviceValueSetter<bool> setter);

    IDeviceBuilder AddTextLabel(string name, string? label, bool? isLabelVisible, DeviceValueGetter<string> getter);

    IDeviceAdapter Build();

    /// <summary>
    /// Set timing related information (the delays NEEO should use when interacting with a device),
    /// which will be used when generating recipes.
    /// </summary>
    /// <param name="timing"><see cref="DeviceTiming"/> specifying delays NEEO should use when interacting with
    /// a device.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    IDeviceBuilder DefineTiming(DeviceTiming timing);

    IDeviceBuilder EnableDiscovery(string headerText, string description, DiscoveryProcess process, bool enableDynamicDeviceBuilder = false);

    IDeviceBuilder EnableNotifications(DeviceNotifierCallback callback);

    IDeviceBuilder EnableRegistration(string headerText, string description, QueryIsRegistered queryIsRegistered, CredentialsRegistrationProcessor processor);

    IDeviceBuilder EnableRegistration(string headerText, string description, QueryIsRegistered queryIsRegistered, SecurityCodeRegistrationProcessor processor);

    /// <summary>
    /// Specify a set of callbacks which allow tracking which devices are currently in use on the NEEO Brain.
    /// This can be used to avoid sending Brain notifications for devices not added on the Brain, to free up
    /// resources and/or remove credentials when the last device is removed.
    /// </summary>
    /// <param name="onDeviceAdded"></param>
    /// <param name="onDeviceRemoved"></param>
    /// <param name="initializeDeviceList"></param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    IDeviceBuilder RegisterDeviceSubscriptionCallbacks(DeviceSubscriptionAction onDeviceAdded, DeviceSubscriptionAction onDeviceRemoved, DeviceListInitializer initializeDeviceList);

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
    /// NEEO supports only two icon overrides (specifically &quot;sonos&quot; and &quot;neeo&quot;).
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
    /// Sets an optional name to use when adding the device to a room.
    /// If not specified, a name based on the type will be used by default, for example: "Accessory".
    /// </summary>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    /// <remarks>Note: This does not apply to devices using discovery.</remarks>
    IDeviceBuilder SetSpecificName(string? specificName);
}

internal sealed class DeviceBuilder : IDeviceBuilder
{
    private static readonly Regex _digitRegex = new(@"^DIGIT \d$", RegexOptions.Compiled);

    private readonly List<string> _additionalSearchTokens = new();
    private readonly HashSet<DeviceCharacteristic> _characteristics = new();
    private int _digitCount;
    private bool _hasInput;

    internal DeviceBuilder(string name, DeviceType type, string? prefix)
    {
        (this.Type, this.Name) = (type, Validator.ValidateString(name));
        this.AdapterName = $"apt-{UniqueNameGenerator.Generate(name, prefix)}";
    }

    public string AdapterName { get; }

    public IReadOnlyCollection<string> AdditionalSearchTokens => this._additionalSearchTokens;

    public ButtonHandler? ButtonHandler { get; private set; }

    public Dictionary<string, Definition> Buttons { get; } = new();

    IReadOnlyCollection<string> IDeviceBuilder.Buttons => this.Buttons.Keys;

    public IReadOnlyCollection<DeviceCharacteristic> Characteristics => this._characteristics;

    IDiscoveryFeature? IDeviceBuilder.DiscoveryController => this.DiscoveryController;

    public DiscoveryFeature? DiscoveryController { get; private set; }

    public uint? DriverVersion { get; private set; }

    public FavoritesController? FavoritesController { get; private set; }

    IFavoritesController? IDeviceBuilder.FavoritesController => this.FavoritesController;

    public bool HasPowerStateSensor => this.Sensors.ContainsKey(Constants.PowerSensorName);

    public DeviceIconOverride? Icon { get; private set; }

    public Dictionary<string, ImageUrlDefinition> ImageUrls { get; } = new();

    IReadOnlyCollection<string> IDeviceBuilder.ImageUrls => this.ImageUrls.Keys;

    public DeviceInitializer? Initializer { get; private set; }

    public string Manufacturer { get; private set; } = "NEEO";

    public string Name { get; }

    public DeviceNotifierCallback? NotifierCallback { get; private set; }

    IRegistrationFeature? IDeviceBuilder.RegistrationController => this.RegistrationController;

    public RegistrationFeature? RegistrationController { get; private set; }

    public Dictionary<string, SensorDefinition> Sensors { get; } = new();

    IReadOnlyCollection<string> IDeviceBuilder.Sensors => this.Sensors.Keys;

    public DeviceSetup Setup { get; } = new DeviceSetup();

    IDeviceSetup IDeviceBuilder.Setup => this.Setup;

    public Dictionary<string, SliderDefinition> Sliders { get; } = new();

    IReadOnlyCollection<string> IDeviceBuilder.Sliders => this.Sliders.Keys;

    public string? SpecificName { get; private set; }

    public SubscriptionController? SubscriptionController { get; private set; }

    ISubscriptionController? IDeviceBuilder.SubscriptionController => this.SubscriptionController;

    public Dictionary<string, SwitchDefinition> Switches { get; } = new();

    IReadOnlyCollection<string> IDeviceBuilder.Switches => this.Switches.Keys;

    public Dictionary<string, TextLabelDefinition> TextLabels { get; } = new();

    IReadOnlyCollection<string> IDeviceBuilder.TextLabels => this.TextLabels.Keys;

    public DeviceTiming? Timing { get; private set; }

    public DeviceType Type { get; }

    IDeviceBuilder IDeviceBuilder.AddAdditionalSearchTokens(string[] tokens) => this.AddAdditionalSearchTokens(tokens);

    IDeviceBuilder IDeviceBuilder.AddButton(string name, string? label) => this.AddButton(name, label);

    IDeviceBuilder IDeviceBuilder.AddButtonHandler(ButtonHandler handler) => this.AddButtonHandler(handler);

    IDeviceBuilder IDeviceBuilder.AddButtons(KnownButtons button) => this.AddButtons(button);

    IDeviceBuilder IDeviceBuilder.AddCharacteristic(DeviceCharacteristic characteristic) => this.AddCharacteristic(characteristic);

    IDeviceBuilder IDeviceBuilder.AddImageUrl(
        string name,
        string? label,
        ImageSize size,
        string? uri,
        DeviceValueGetter<string>? getter
    ) => this.AddImageUrl(name, label, size, uri, getter);

    IDeviceBuilder IDeviceBuilder.AddPowerStateSensor(DeviceValueGetter<bool> sensor) => this.AddPowerStateSensor(sensor);

    IDeviceBuilder IDeviceBuilder.AddSensor(
        string name,
        string? label,
        DeviceValueGetter<double> getter,
        double rangeLow,
        double rangeHigh,
        string units
    ) => this.AddSensor(name, label, getter, rangeLow, rangeHigh, units);

    IDeviceBuilder IDeviceBuilder.AddSensor(
        string name,
        string? label,
        DeviceValueGetter<string> getter
    ) => this.AddSensor(name, label, getter);

    IDeviceBuilder IDeviceBuilder.AddSensor(
        string name,
        string? label,
        DeviceValueGetter<bool> getter
    ) => this.AddSensor(name, label, getter);

    IDeviceBuilder IDeviceBuilder.AddSlider(
        string name,
        string? label,
        DeviceValueGetter<double> getter,
        DeviceValueSetter<double> setter,
        double rangeLow,
        double rangeHigh,
        string unit
    ) => this.AddSlider(name, label, getter, setter, rangeLow, rangeHigh, unit);

    IDeviceBuilder IDeviceBuilder.AddSwitch(
        string name,
        string? label,
        DeviceValueGetter<bool> getter,
        DeviceValueSetter<bool> setter
    ) => this.AddSwitch(name, label, getter, setter);

    IDeviceBuilder IDeviceBuilder.AddTextLabel(
       string name,
       string? label,
       bool? isLabelVisible,
       DeviceValueGetter<string> getter
    ) => this.AddTextLabel(name, label, isLabelVisible, getter);

    IDeviceAdapter IDeviceBuilder.Build() => this.BuildAdapter();

    IDeviceBuilder IDeviceBuilder.DefineTiming(DeviceTiming timing) => this.DefineTiming(timing);

    IDeviceBuilder IDeviceBuilder.EnableDiscovery(
        string headerText,
        string description,
        DiscoveryProcess process,
        bool enableDynamicDeviceBuilder
    ) => this.EnableDiscovery(headerText, description, process, enableDynamicDeviceBuilder);

    IDeviceBuilder IDeviceBuilder.EnableNotifications(DeviceNotifierCallback callback) => this.EnableNotifications(callback);

    IDeviceBuilder IDeviceBuilder.EnableRegistration(
        string headerText,
        string description,
        QueryIsRegistered queryIsRegistered,
        CredentialsRegistrationProcessor processor
    ) => processor == null ? throw new ArgumentNullException(nameof(processor)) : this.EnableRegistration(
        headerText,
        description,
        RegistrationType.Credentials,
        queryIsRegistered,
        (Credentials credentials) => processor(credentials)
    );

    IDeviceBuilder IDeviceBuilder.EnableRegistration(
        string headerText,
        string description,
        QueryIsRegistered queryIsRegistered,
        SecurityCodeRegistrationProcessor processor
    ) => processor == null ? throw new ArgumentNullException(nameof(processor)) : this.EnableRegistration(
        headerText,
        description,
        RegistrationType.SecurityCode,
        queryIsRegistered,
        (SecurityCodeContainer container) => processor(container.SecurityCode)
    );

    IDeviceBuilder IDeviceBuilder.RegisterDeviceSubscriptionCallbacks(
        DeviceSubscriptionAction onDeviceAdded,
        DeviceSubscriptionAction onDeviceRemoved,
        DeviceListInitializer initializeDeviceList
    ) => this.RegisterDeviceSubscriptionCallbacks(onDeviceAdded, onDeviceRemoved, initializeDeviceList);

    IDeviceBuilder IDeviceBuilder.RegisterFavoritesHandler(FavoritesHandler handler) => this.RegisterFavoritesHandler(handler);

    IDeviceBuilder IDeviceBuilder.RegisterInitializer(DeviceInitializer initializer) => this.RegisterInitializer(initializer);

    IDeviceBuilder IDeviceBuilder.SetDriverVersion(uint version) => this.SetDriverVersion(version);

    IDeviceBuilder IDeviceBuilder.SetIcon(DeviceIconOverride icon) => this.SetIcon(icon);

    IDeviceBuilder IDeviceBuilder.SetManufacturer(string manufacturer) => this.SetManufacturer(manufacturer);

    IDeviceBuilder IDeviceBuilder.SetSpecificName(string? specificName) => this.SetSpecificName(specificName);

    private DeviceBuilder AddAdditionalSearchTokens(string[] tokens)
    {
        this._additionalSearchTokens.AddRange(tokens);
        return this;
    }

    private DeviceBuilder AddButton(string name, string? label = default)
    {
        Definition definition = new(
            Validator.ValidateString(name),
            Validator.ValidateString(label, allowNull: true)
        );
        if (!this.Buttons.TryAdd(name, definition))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        bool isInput = name.StartsWith(Constants.InputPrefix);
        this._hasInput |= isInput;
        if (!isInput && DeviceBuilder._digitRegex.IsMatch(name))
        {
            this._digitCount++;
        }
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

    private DeviceBuilder AddCharacteristic(DeviceCharacteristic characteristic)
    {
        this._characteristics.Add(characteristic);
        return this;
    }

    private DeviceBuilder AddImageUrl(string name, string? label, ImageSize size, string? uri, DeviceValueGetter<string>? getter)
    {
        ImageUrlDefinition definition = new(
            Validator.ValidateString(name),
            Validator.ValidateString(label, allowNull: true),
            ValueController.Create((getter, uri) switch
            {
                (null, null) => throw new InvalidOperationException($"Either {nameof(uri)} or {nameof(getter)} must be specified."),
                ({ }, _) => getter,
                (null, string) => _ => Task.FromResult(uri)
            }),
            size,
            uri
        );
        if (!this.ImageUrls.TryAdd(name, definition))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        return this;
    }

    private DeviceBuilder AddPowerStateSensor(DeviceValueGetter<bool> getter)
    {
        SensorDefinition definition = new(
            SensorTypes.Power,
            Constants.PowerSensorName,
            Constants.PowerSensorLabel,
            ValueController.Create(getter)
        );
        if (!this.Sensors.TryAdd(Constants.PowerSensorName, definition))
        {
            throw new InvalidOperationException("PowerState sensor already added.");
        }
        return this;
    }

    private DeviceBuilder AddSensor(SensorTypes type, string name, string? label, ValueController controller)
    {
        if (name == Constants.PowerSensorName)
        {
            throw new ArgumentException($"Name can not be {Constants.PowerSensorName}.", nameof(name));
        }
        SensorDefinition definition = new(
            type,
            Validator.ValidateString(name),
            Validator.ValidateString(label, allowNull: true),
            controller
        );
        if (!this.Sensors.TryAdd(name, definition))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        return this;
    }

    private DeviceBuilder AddSensor(string name, string? label, DeviceValueGetter<bool> getter) => this.AddSensor(SensorTypes.Binary, name, label, ValueController.Create(getter));

    private DeviceBuilder AddSensor(string name, string? label, DeviceValueGetter<string> getter) => this.AddSensor(SensorTypes.String, name, label, ValueController.Create(getter));

    private DeviceBuilder AddSensor(string name, string? label, DeviceValueGetter<double> getter, double rangeLow, double rangeHigh, string unit)
    {
        if (name == Constants.PowerSensorName)
        {
            throw new ArgumentException($"Name can not be {Constants.PowerSensorName}.", nameof(name));
        }
        RangeSensorDefinition definition = new(
            Validator.ValidateString(name),
            Validator.ValidateString(label, allowNull: true),
            ValueController.Create(getter),
            Validator.ValidateRange(rangeLow, rangeHigh),
            Validator.ValidateString(unit)
        );
        if (!this.Sensors.TryAdd(name, definition))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        return this;
    }

    private DeviceBuilder AddSlider(string name, string? label, DeviceValueGetter<double> getter, DeviceValueSetter<double> setter, double rangeLow, double rangeHigh, string unit)
    {
        SliderDefinition definition = new(
            Validator.ValidateString(name),
            Validator.ValidateString(label, allowNull: true),
            ValueController.Create(getter, setter),
            Validator.ValidateRange(rangeLow, rangeHigh),
            Validator.ValidateString(unit)
        );
        if (!this.Sliders.TryAdd(name, definition))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        return this;
    }

    private DeviceBuilder AddSwitch(string name, string? label, DeviceValueGetter<bool> getter, DeviceValueSetter<bool> setter)
    {
        SwitchDefinition definition = new(
            Validator.ValidateString(name),
            Validator.ValidateString(label, allowNull: true),
            ValueController.Create(getter, setter)
        );
        if (!this.Switches.TryAdd(name, definition))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        return this;
    }

    private DeviceBuilder AddTextLabel(string name, string? label, bool? isLabelVisible, DeviceValueGetter<string> getter)
    {
        TextLabelDefinition definition = new(
            Validator.ValidateString(name),
            Validator.ValidateString(label, allowNull: true),
            ValueController.Create(getter),
            isLabelVisible
        );
        if (!this.TextLabels.TryAdd(name, definition))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        return this;
    }

    private DeviceAdapter BuildAdapter()
    {
        if (this.ButtonHandler == null && this.Buttons.Any())
        {
            throw new InvalidOperationException($"There are buttons defined but no handler was specified (by calling {nameof(IDeviceBuilder.AddButtonHandler)}.");
        }
        if (this.Type.RequiresInput() && !this._hasInput)
        {
            throw new InvalidOperationException($"No input buttons defined - note that input button names must begin with \"{Constants.InputPrefix}\".");
        }
        if (this.Characteristics.Contains(DeviceCharacteristic.BridgeDevice) && this.Setup.RegistrationType is null)
        {
            throw new InvalidOperationException($"A device with characteristic {DeviceCharacteristic.BridgeDevice} must support registration (by calling {nameof(IDeviceBuilder.EnableRegistration)}).");
        }
        List<DeviceCapability> deviceCapabilities = this.Characteristics.Select(characteristic => (DeviceCapability)characteristic).ToList();
        string pathPrefix = $"/device/{this.AdapterName}/";
        HashSet<string> paths = new();
        List<Component> capabilities = new();
        Dictionary<string, IFeature> handlers = new();
        foreach ((string name, string? label) in this.Buttons.Values)
        {
            AddCapability(BuildButton(pathPrefix, name, label), new ButtonFeature(this.ButtonHandler!, name));
        }
        foreach ((string name, string? label, ValueController controller, IReadOnlyCollection<double> range, string unit) in this.Sliders.Values)
        {
            AddCapability(BuildSensor(pathPrefix, name, label, new RangeSensorDetails(range, unit)), controller);
            AddCapability(BuildSlider(pathPrefix, name, label, range, unit), controller);
        }
        foreach ((string name, string? label, ValueController controller) in this.Switches.Values)
        {
            AddCapability(BuildSensor(pathPrefix, name, label, new(SensorTypes.Binary)), controller);
            AddCapability(BuildSwitch(pathPrefix, name, label), controller);
        }
        foreach ((string name, string? label, ValueController controller, bool? isLabelVisible) in this.TextLabels.Values)
        {
            AddCapability(BuildSensor(pathPrefix, name, label, new(SensorTypes.String)), controller);
            AddCapability(BuildTextLabel(pathPrefix, name, label, isLabelVisible), controller);
        }
        foreach ((string name, string? label, ValueController controller, ImageSize size, string? uri) in this.ImageUrls.Values)
        {
            AddCapability(BuildSensor(pathPrefix, name, label, new(SensorTypes.String)), controller);
            AddCapability(BuildImageUrl(pathPrefix, name, label, size, uri), controller);
        }
        foreach (SensorDefinition definition in this.Sensors.Values)
        {
            AddCapability(
                definition switch
                {
                    { Type: SensorTypes.Power } => BuildPowerSensor(pathPrefix),
                    RangeSensorDefinition rsd => BuildSensor(pathPrefix, rsd.Name, rsd.Label, new RangeSensorDetails(rsd.Range, rsd.Unit)),
                    _ => BuildSensor(pathPrefix, definition.Name, definition.Label, new(definition.Type))
                },
                definition.Controller
            );
        }
        if (this.DiscoveryController is { } discoveryController)
        {
            AddRouteHandler(BuildComponent(pathPrefix, ComponentType.Discovery), discoveryController);
            if (this.RegistrationController is { } registrationController)
            {
                deviceCapabilities.Add(DeviceCapability.RegisterUserAccount);
                AddRouteHandler(BuildComponent(pathPrefix, ComponentType.Registration), registrationController);
            }
        }
        else if (!deviceCapabilities.Contains(DeviceCapability.DynamicDevice) && deviceCapabilities.FindIndex(RequiresDiscovery) is int index and > -1)
        {
            throw new InvalidOperationException($"Discovery required for {deviceCapabilities[index]}.");
        }
        if (this.FavoritesController is { } favoritesController)
        {
            if (this._digitCount != 10)
            {
                throw new InvalidOperationException("Can not enable favorites without the 10 digit buttons being added. It is highly recommended to call AddButtonGroup(ButtonGroup.NumberPad).");
            }
            deviceCapabilities.Add(DeviceCapability.CustomFavoriteHandler);
            AddRouteHandler(BuildComponent(pathPrefix, ComponentType.FavoritesHandler), favoritesController);
        }
        if (this.SubscriptionController is { } subscriptionController)
        {
            AddRouteHandler(BuildComponent(pathPrefix, ComponentType.Subscription), subscriptionController);
        }
        return new(
            this.AdapterName,
            capabilities,
            handlers,
            deviceCapabilities,
            this.Name,
            this.DriverVersion,
            this.Icon,
            this.Initializer,
            this.Manufacturer,
            this.Setup,
            this.SpecificName,
            this.Timing ?? new(),
            this.AdditionalSearchTokens,
            this.Type
        );

        void AddCapability(Component capability, IFeature controller)
        {
            AddRouteHandler(capability, controller);
            capabilities.Add(capability);
        }

        void AddRouteHandler(Component capability, IFeature controller)
        {
            handlers[Uri.UnescapeDataString(capability.Name)] = paths.Add(capability.Path)
                ? controller
                : throw new InvalidOperationException($"Path {capability.Path} is not unique.");
        }

        static Component BuildButton(string pathPrefix, string featureName, string? label)
        {
            string name = Uri.EscapeDataString(featureName);
            string path = pathPrefix + name;
            return new(ComponentType.Button, name, label is { } text ? Uri.EscapeDataString(text) : name, path);
        }

        static Component BuildComponent(string pathPrefix, ComponentType type)
        {
            string name = Uri.EscapeDataString(TextAttribute.GetText(type));
            string path = pathPrefix + name;
            return new(type, name, default, path);
        }

        static ImageUrlComponent BuildImageUrl(string pathPrefix, string featureName, string? label, ImageSize size, string? uri)
        {
            string name = Uri.EscapeDataString(featureName);
            string path = pathPrefix + name;
            return new(
                name,
                label is { } text ? Uri.EscapeDataString(text) : name,
                path,
                uri,
                size,
                GetSensorName(name)
            );
        }

        static SensorComponent BuildPowerSensor(string pathPrefix)
        {
            SensorComponent component = BuildSensor(pathPrefix, string.Empty, Constants.PowerSensorLabel, new(SensorTypes.Power));
            /*
                Power state sensors are added by AddPowerStateSensor with the name "powerstate".
                For backward compatibility, we need to avoid changing it to "POWERSTATE_SENSOR".
            */
            string legacyNoSuffixName = Uri.EscapeDataString(Constants.PowerSensorName);
            return component with { Name = legacyNoSuffixName, Path = pathPrefix + legacyNoSuffixName };
        }

        static TextLabelComponent BuildTextLabel(string pathPrefix, string featureName, string? label, bool? isLabelVisible)
        {
            string name = Uri.EscapeDataString(featureName);
            string path = pathPrefix + name;
            return new(name, label is { } text ? Uri.EscapeDataString(text) : name, path, isLabelVisible, GetSensorName(name));
        }

        static SensorComponent BuildSensor(string pathPrefix, string featureName, string? label, SensorDetails sensor)
        {
            string name = GetSensorName(Uri.EscapeDataString(featureName));
            string path = pathPrefix + name;
            return new(name, Uri.EscapeDataString(label ?? name), path, sensor);
        }

        static SliderComponent BuildSlider(string pathPrefix, string featureName, string? label, IReadOnlyCollection<double> range, string unit)
        {
            string name = Uri.EscapeDataString(featureName);
            string path = pathPrefix + name;
            return new(name, label is { } text ? Uri.EscapeDataString(text) : name, path, new(range, unit, GetSensorName(name)));
        }

        static SwitchComponent BuildSwitch(string pathPrefix, string featureName, string? label)
        {
            string name = Uri.EscapeDataString(featureName);
            string path = pathPrefix + name;
            return new(name, Uri.EscapeDataString(label ?? string.Empty), path, GetSensorName(name));
        }

        static string GetSensorName(string name) => name.EndsWith(SensorDetails.ComponentSuffix) ? name : string.Concat(name.ToUpperInvariant(), SensorDetails.ComponentSuffix);

        static bool RequiresDiscovery(DeviceCapability capability) => capability is DeviceCapability.BridgeDevice or DeviceCapability.AddAnotherDevice or DeviceCapability.RegisterUserAccount;
    }

    private DeviceBuilder DefineTiming(DeviceTiming timing)
    {
        if (!this.Type.SupportsTiming())
        {
            throw new NotSupportedException($"Device type {this.Type} does not support timing.");
        }
        if (this.Timing.HasValue)
        {
            throw new InvalidOperationException("Timing is already defined.");
        }
        this.Timing = timing;
        return this;
    }

    private DeviceBuilder EnableDiscovery(string headerText, string description, DiscoveryProcess process, bool enableDynamicDeviceBuilder)
    {
        if (this.Setup.Discovery is not null)
        {
            throw new InvalidOperationException("Discovery is already defined.");
        }
        Validator.ValidateString(this.Setup.DiscoveryHeaderText = headerText, maxLength: 255, name: nameof(headerText));
        Validator.ValidateString(this.Setup.DiscoveryDescription = description, maxLength: 255, name: nameof(description));
        this.Setup.Discovery = true;
        this.Setup.EnableDynamicDeviceBuilder = enableDynamicDeviceBuilder;
        this.DiscoveryController = new DiscoveryFeature(process ?? throw new ArgumentNullException(nameof(process)), enableDynamicDeviceBuilder);
        return this;
    }

    private DeviceBuilder EnableNotifications(DeviceNotifierCallback callback)
    {
        if (this.NotifierCallback != null)
        {
            throw new InvalidOperationException($"{nameof(DeviceNotifierCallback)} already defined.");
        }
        this.NotifierCallback = callback;
        return this;
    }

    private DeviceBuilder EnableRegistration<TPayload>(string headerText, string description, RegistrationType type, QueryIsRegistered queryIsRegistered, Func<TPayload, Task<RegistrationResult>> processor)
            where TPayload : notnull
    {
        if (this.Setup.Discovery is null)
        {
            throw new InvalidOperationException($"Registration is only supported on devices with discovery. (Call {nameof(IDeviceBuilder.EnableDiscovery)} first).");
        }
        if (this.Setup.RegistrationType is not null)
        {
            throw new InvalidOperationException("Registration is already defined.");
        }
        this.Setup.RegistrationType = type;
        this.RegistrationController = new(queryIsRegistered, type, element => processor(element.Deserialize<TPayload>(JsonSerialization.Options)!));
        Validator.ValidateString(this.Setup.RegistrationDescription = description, maxLength: 255, name: nameof(description));
        Validator.ValidateString(this.Setup.RegistrationHeaderText = headerText, maxLength: 255, name: nameof(headerText));
        return this;
    }

    private DeviceBuilder RegisterDeviceSubscriptionCallbacks(DeviceSubscriptionAction onDeviceAdded, DeviceSubscriptionAction onDeviceRemoved, DeviceListInitializer initializeDeviceList)
    {
        if (this.SubscriptionController != null)
        {
            throw new InvalidOperationException($"{nameof(this.RegisterDeviceSubscriptionCallbacks)} was already called.");
        }
        this.SubscriptionController = new(
            onDeviceAdded ?? throw new ArgumentNullException(nameof(onDeviceAdded)),
            onDeviceRemoved ?? throw new ArgumentNullException(nameof(onDeviceRemoved)),
            initializeDeviceList ?? throw new ArgumentNullException(nameof(initializeDeviceList))
        );
        return this;
    }

    private DeviceBuilder RegisterFavoritesHandler(FavoritesHandler handler)
    {
        if (this.FavoritesController != null)
        {
            throw new InvalidOperationException($"{nameof(FavoritesHandler)} already defined.");
        }
        if (!this.Type.SupportsFavorites())
        {
            throw new NotSupportedException($"Device type {this.Type} does not support favorites.");
        }
        this.FavoritesController = new(handler ?? throw new ArgumentNullException(nameof(handler)));
        return this;
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

    private DeviceBuilder SetManufacturer(string manufacturer)
    {
        Validator.ValidateString(this.Manufacturer = manufacturer);
        return this;
    }

    private DeviceBuilder SetSpecificName(string? specificName)
    {
        Validator.ValidateString(this.SpecificName = specificName, allowNull: true);
        return this;
    }

    private sealed record class SecurityCodeContainer(String SecurityCode);

    public record Definition(string Name, string? Label);

    public sealed record ImageUrlDefinition(string Name, string? Label, ValueController Controller, ImageSize Size, String? Uri) : Definition(Name, Label);

    public sealed record TextLabelDefinition(string Name, string? Label, ValueController Controller, bool? IsLabelVisible) : Definition(Name, Label);

    public record SensorDefinition(SensorTypes Type, string Name, string? Label, ValueController Controller) : Definition(Name, Label);

    public sealed record RangeSensorDefinition(string Name, string? Label, ValueController Controller, IReadOnlyCollection<double> Range, string Unit) : SensorDefinition(SensorTypes.Range, Name, Label, Controller);

    public sealed record SliderDefinition(string Name, string? Label, ValueController Conroller, IReadOnlyCollection<double> Range, string Unit) : Definition(Name, Label);

    public sealed record SwitchDefinition(string Name, string? Label, ValueController Controller) : Definition(Name, Label);
}