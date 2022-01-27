﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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
    IReadOnlyCollection<DeviceFeature> Buttons { get; }

    /// <summary>
    /// Gets the collection of special characteristics of the device.
    /// </summary>
    IReadOnlyCollection<DeviceCharacteristic> Characteristics { get; }

    /// <summary>
    /// Gets a set of callbacks which allow tracking which devices are currently in use on the NEEO Brain.
    /// This can be used to avoid sending Brain notifications for devices not added on the Brain, to free up
    /// resource and/or remove credentials when the last device is removed.
    /// </summary>
    ISubscriptionController? SubscriptionController { get; }

    DiscoveryProcess? DiscoveryProcess { get; }

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
    IReadOnlyDictionary<DeviceFeature, IValueController> ImageUrls { get; }

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

    IRegistrationController? RegistrationController { get; }

    /// <summary>
    /// Gets the collection of sensors defined for the device.
    /// </summary>
    IReadOnlyDictionary<DeviceFeature, IValueController> Sensors { get; }

    IDeviceSetup Setup { get; }

    /// <summary>
    /// Gets the collection of sliders defined for the device.
    /// </summary>
    IReadOnlyDictionary<DeviceFeature, IValueController> Sliders { get; }

    /// <summary>
    /// Gets an optional name to use when adding the device to a room
    /// (a name based on the type will be used by default, for example: 'Accessory').
    /// </summary>
    /// <remarks>Note: This does not apply to devices using discovery.</remarks>
    string? SpecificName { get; }

    DeviceNotifierCallback? NotifierCallback { get; }

    /// <summary>
    /// Gets the collection of switches defined for the device.
    /// </summary>
    IReadOnlyDictionary<DeviceFeature, IValueController> Switches { get; }

    /// <summary>
    /// Gets the collection of text labels defined for the device.
    /// </summary>
    IReadOnlyDictionary<DeviceFeature, IValueController> TextLabels { get; }

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
        double rangeLow = 0,
        double rangeHigh = 100,
        string units = "%"
    );

    IDeviceBuilder AddSlider(
        string name,
        string? label,
        DeviceValueGetter<double> getter,
        DeviceValueSetter<double> setter,
        double rangeLow = 0,
        double rangeHigh = 100,
        string units = "%"
    );

    IDeviceBuilder AddSwitch(string name, string? label, DeviceValueGetter<bool> getter, DeviceValueSetter<bool> setter);

    IDeviceBuilder AddTextLabel(string name, string? label, bool? isLabelVisible, DeviceValueGetter<string> getter);

    /// <summary>
    /// Set timing related information (the delays NEEO should use when interacting with a device),
    /// which will be used when generating recipes.
    /// </summary>
    /// <param name="timing"><see cref="DeviceTiming"/> specifying delays NEEO should use when interacting with
    /// a device.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    IDeviceBuilder DefineTiming(DeviceTiming timing);

    IDeviceBuilder EnableDiscovery(string headerText, string description, DiscoveryProcess process, bool enableDynamicDeviceBuilder = false);

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

    IDeviceBuilder EnableNotifications(DeviceNotifierCallback callback);

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
    private readonly List<string> _additionalSearchTokens = new();
    private readonly HashSet<DeviceCharacteristic> _characteristics = new();
    private readonly IReadOnlyDictionary<DeviceFeature, IValueController> _imageUrlsReadOnly;
    private readonly IReadOnlyDictionary<DeviceFeature, IValueController> _slidersReadOnly;
    private readonly IReadOnlyDictionary<DeviceFeature, IValueController> _switchesReadOnly;
    private readonly IReadOnlyDictionary<DeviceFeature, IValueController> _textLabelsReadOnly;

    internal DeviceBuilder(string name, DeviceType type, string? prefix)
    {
        this.Type = type;
        Validator.ValidateString(this.Name = name, name: nameof(name));
        this.AdapterName = $"apt-{UniqueNameGenerator.Generate(name, prefix)}";
        this._imageUrlsReadOnly = new CovariantReadOnlyDictionary<DeviceFeature, ValueController<string>, IValueController>(this.ImageUrls);
        this._slidersReadOnly = new CovariantReadOnlyDictionary<DeviceFeature, ValueController<double>, IValueController>(this.Sliders);
        this._switchesReadOnly = new CovariantReadOnlyDictionary<DeviceFeature, ValueController<bool>, IValueController>(this.Switches);
        this._textLabelsReadOnly = new CovariantReadOnlyDictionary<DeviceFeature, ValueController<string>, IValueController>(this.TextLabels);
    }

    public string AdapterName { get; }

    public IReadOnlyCollection<string> AdditionalSearchTokens => this._additionalSearchTokens;

    public ButtonHandler? ButtonHandler { get; private set; }

    public Collection<DeviceFeature> Buttons { get; } = new();

    IReadOnlyCollection<DeviceFeature> IDeviceBuilder.Buttons => this.Buttons;

    public IReadOnlyCollection<DeviceCharacteristic> Characteristics => this._characteristics;

    public SubscriptionController? SubscriptionController { get; private set; }

    ISubscriptionController? IDeviceBuilder.SubscriptionController => this.SubscriptionController;

    public DiscoveryProcess? DiscoveryProcess { get; private set; }

    public uint? DriverVersion { get; private set; }

    public FavoritesController? FavoritesController { get; private set; }

    IFavoritesController? IDeviceBuilder.FavoritesController => this.FavoritesController;

    public bool HasPowerStateSensor => this.Sensors.Keys.Any(static component => component.Type == ComponentType.Power);

    public DeviceIconOverride? Icon { get; private set; }

    public Dictionary<DeviceFeature, ValueController<string>> ImageUrls { get; } = new();

    IReadOnlyDictionary<DeviceFeature, IValueController> IDeviceBuilder.ImageUrls => this._imageUrlsReadOnly;

    public DeviceInitializer? Initializer { get; private set; }

    public string Manufacturer { get; private set; } = "NEEO";

    public string Name { get; }

    IRegistrationController? IDeviceBuilder.RegistrationController => this.RegistrationController;

    public RegistrationController? RegistrationController { get; private set; }

    public Dictionary<DeviceFeature, IValueController> Sensors { get; } = new();

    IReadOnlyDictionary<DeviceFeature, IValueController> IDeviceBuilder.Sensors => this.Sensors;

    public DeviceSetup Setup { get; } = new DeviceSetup();

    IDeviceSetup IDeviceBuilder.Setup => this.Setup;

    public Dictionary<DeviceFeature, ValueController<double>> Sliders { get; } = new();

    IReadOnlyDictionary<DeviceFeature, IValueController> IDeviceBuilder.Sliders => this._slidersReadOnly;

    public string? SpecificName { get; private set; }

    public DeviceNotifierCallback? NotifierCallback { get; private set; }

    public Dictionary<DeviceFeature, ValueController<bool>> Switches { get; } = new();

    IReadOnlyDictionary<DeviceFeature, IValueController> IDeviceBuilder.Switches => this._switchesReadOnly;

    public Dictionary<DeviceFeature, ValueController<string>> TextLabels { get; } = new();

    IReadOnlyDictionary<DeviceFeature, IValueController> IDeviceBuilder.TextLabels => this._textLabelsReadOnly;

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

    IDeviceBuilder IDeviceBuilder.AddSlider(
        string name,
        string? label,
        DeviceValueGetter<double> getter,
        DeviceValueSetter<double> setter,
        double rangeLow,
        double rangeHigh,
        string units
    ) => this.AddSlider(name, label, getter, setter, rangeLow, rangeHigh, units);

    IDeviceBuilder IDeviceBuilder.AddSwitch(
        string name,
        string? label,
        DeviceValueGetter<bool> getter,
        DeviceValueSetter<bool> setter
    ) => this.AddSwitch(name, label, getter, setter);

    IDeviceBuilder IDeviceBuilder.AddSensor(
        string name,
        string? label,
        DeviceValueGetter<double> getter,
        double rangeLow,
        double rangeHigh,
        string units
    ) => this.AddSensor(name, label, getter, rangeLow, rangeHigh, units);

    IDeviceBuilder IDeviceBuilder.AddTextLabel(
       string name,
       string? label,
       bool? isLabelVisible,
       DeviceValueGetter<string> getter
    ) => this.AddTextLabel(name, label, isLabelVisible, getter);

    IDeviceBuilder IDeviceBuilder.DefineTiming(DeviceTiming timing) => this.DefineTiming(timing);

    IDeviceBuilder IDeviceBuilder.EnableDiscovery(
        string headerText,
        string description,
        DiscoveryProcess process,
        bool enableDynamicDeviceBuilder
    ) => this.EnableDiscovery(headerText, description, process, enableDynamicDeviceBuilder);

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

    IDeviceBuilder IDeviceBuilder.EnableNotifications(DeviceNotifierCallback callback) => this.EnableNotifications(callback);

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
        Validator.ValidateString(name);
        Validator.ValidateString(label, allowNull: true);
        int index = this.Buttons.BinarySearchByValue(name, static feature => feature.Name, StringComparer.OrdinalIgnoreCase);
        if (index >= 0)
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        DeviceFeature feature = new(ComponentType.Button, name, label);
        this.Buttons.Insert(~index, feature);
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
        if (uri == null && getter == null)
        {
            throw new InvalidOperationException($"Either {nameof(uri)} or {nameof(getter)} must be specified.");
        }
        this.ImageUrls.Add(
            new(ComponentType.ImageUrl, name, label, Uri: uri, Size: size),
            new(getter ?? new((_) => Task.FromResult(uri!)))
        );
        return this;
    }

    private DeviceBuilder AddPowerStateSensor(DeviceValueGetter<bool> getter)
    {
        if (this.HasPowerStateSensor)
        {
            throw new InvalidOperationException("PowerStateSensor already added.");
        }
        this.Sensors.Add(
            new(ComponentType.Power, Constants.PowerSensorName, Constants.PowerSensorLabel, SensorType: SensorTypes.Power),
            new ValueController<bool>(getter)
        );
        return this;
    }

    private DeviceBuilder AddSensor(string name, string? label, DeviceValueGetter<double> getter, double rangeLow, double rangeHigh, string units)
    {
        if (name == Constants.PowerSensorName)
        {
            throw new ArgumentException($"Name can not be {Constants.PowerSensorName}.", nameof(name));
        }
        this.Sensors.Add(
            new(ComponentType.Sensor, name, label, RangeLow: rangeLow, RangeHigh: rangeHigh, Unit: units),
            new ValueController<double>(getter)
        );
        return this;
    }

    private DeviceBuilder AddSlider(string name, string? label, DeviceValueGetter<double> getter, DeviceValueSetter<double> setter, double rangeLow, double rangeHigh, string units)
    {
        this.Sliders.Add(
            new(ComponentType.Slider, name, label, RangeLow: rangeLow, RangeHigh: rangeHigh, Unit: units, SensorType: SensorTypes.Range),
            new(getter, setter ?? throw new ArgumentNullException(nameof(setter)))
        );
        return this;
    }

    private DeviceBuilder AddSwitch(string name, string? label, DeviceValueGetter<bool> getter, DeviceValueSetter<bool> setter)
    {
        this.Switches.Add(
            new(ComponentType.Switch, name, label),
            new(getter, setter ?? throw new ArgumentNullException(nameof(setter)))
        );
        return this;
    }

    private DeviceBuilder AddTextLabel(string name, string? label, bool? isLabelVisible, DeviceValueGetter<string> getter)
    {
        this.TextLabels.Add(
            new(ComponentType.TextLabel, name, label, IsLabelVisible: isLabelVisible),
            new(getter)
        );
        return this;
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
        this.DiscoveryProcess = process ?? throw new ArgumentNullException(nameof(process));
        this.Setup.Discovery = true;
        this.Setup.EnableDynamicDeviceBuilder = enableDynamicDeviceBuilder;
        return this;
    }

    private DeviceBuilder EnableRegistration<TPayload>(string headerText, string description, RegistrationType type, QueryIsRegistered queryIsRegistered, Func<TPayload, Task> processor)
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
        this.RegistrationController = new(queryIsRegistered, element => processor(element.Deserialize<TPayload>(JsonSerialization.Options)!));
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

    private DeviceBuilder EnableNotifications(DeviceNotifierCallback callback)
    {
        if (this.NotifierCallback != null)
        {
            throw new InvalidOperationException($"{nameof(DeviceNotifierCallback)} already defined.");
        }
        this.NotifierCallback = callback;
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

    private sealed record class SecurityCodeContainer(String SecurityCode);
}