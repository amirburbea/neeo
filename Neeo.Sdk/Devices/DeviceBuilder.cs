using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Components;
using Neeo.Sdk.Devices.Discovery;
using Neeo.Sdk.Devices.Features;
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

    IReadOnlyCollection<string> Directories { get; }

    IDiscoveryFeature? DiscoveryFeature { get; }

    /// <summary>
    /// Version of the device driver.
    /// Incrementing this version will cause the brain to query for new components.
    /// </summary>
    int? DriverVersion { get; }

    IFavoritesFeature? FavoritesFeature { get; }

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

    IRegistrationFeature? RegistrationFeature { get; }

    /// <summary>
    /// Gets the name of the sensors defined for the device.
    /// </summary>
    IReadOnlyCollection<string> Sensors { get; }

    DeviceSetup Setup { get; }

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
    ISubscriptionFeature? SubscriptionFeature { get; }

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

    IDeviceBuilder AddAdditionalSearchTokens(params string[] tokens) => this.AddAdditionalSearchTokens((IEnumerable<string>)tokens);

    IDeviceBuilder AddAdditionalSearchTokens(IEnumerable<string> tokens);

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
    /// Add a button (or bitwise combination of buttons) to the device.
    /// </summary>
    /// <param name="buttons">The button (or bitwise combination of buttons) to add.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    /// <remarks>Note that adding buttons to the device requires defining a button handler via
    /// <see cref="AddButtonHandler"/>.</remarks>
    IDeviceBuilder AddButton(KnownButtons buttons);

    /// <summary>
    /// Add a group of buttons to the device.
    /// </summary>
    /// <param name="group">The <see cref="ButtonGroups"/> to add.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    /// <remarks>Note that adding buttons to the device requires defining a button handler via
    /// <see cref="AddButtonHandler"/>.</remarks>
    IDeviceBuilder AddButtonGroup(ButtonGroups group);

    /// <summary>
    /// Sets a callback to be invoked in response to calls from the NEEO Brain to handle button presses.
    /// </summary>
    /// <param name="handler">The button handler callback.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    IDeviceBuilder AddButtonHandler(ButtonHandler handler);

    IDeviceBuilder AddCharacteristic(DeviceCharacteristic characteristic);

    IDeviceBuilder AddDirectory(
        string name,
        string? label,
        DirectoryRole? role,
        DeviceDirectoryPopulator populator,
        DirectoryActionHandler actionHandler
    );

    /// <summary>
    /// Sets a callback to be invoked in response to calls from the NEEO Brain to handle launching favorites.
    /// </summary>
    /// <param name="handler">The favorites handler callback.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    IDeviceBuilder AddFavoriteHandler(FavoriteHandler handler);

    IDeviceBuilder AddImageUrl(
            string name,
        string? label,
        ImageSize size,
        string? uri = default,
        DeviceValueGetter<string>? getter = default
    );

    IDeviceBuilder AddPlayerWidget(IPlayerWidgetCallbacks callbacks);

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

    IDeviceAdapter BuildAdapter();

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
    IDeviceBuilder RegisterDeviceSubscriptionCallbacks(DeviceSubscriptionHandler onDeviceAdded, DeviceSubscriptionHandler onDeviceRemoved, DeviceSubscriptionListHandler initializeDeviceList);

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
    IDeviceBuilder SetDriverVersion(int? version);

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
    private bool _hasPlayerWidget;
    private int _roles;

    internal DeviceBuilder(string name, DeviceType type, string? prefix)
    {
        (this.Type, this.Name) = (type, Validator.ValidateText(name));
        this.AdapterName = $"apt-{UniqueNameGenerator.Generate(name, prefix)}";
    }

    public string AdapterName { get; }

    public IReadOnlyCollection<string> AdditionalSearchTokens => this._additionalSearchTokens;

    public ButtonHandler? ButtonHandler { get; private set; }

    public Dictionary<string, ButtonConstruct> Buttons { get; } = new();

    IReadOnlyCollection<string> IDeviceBuilder.Buttons => this.Buttons.Keys;

    public IReadOnlyCollection<DeviceCharacteristic> Characteristics => this._characteristics;

    public Dictionary<string, DirectoryConstruct> Directories { get; } = new();

    IReadOnlyCollection<string> IDeviceBuilder.Directories => this.Directories.Keys;

    IDiscoveryFeature? IDeviceBuilder.DiscoveryFeature => this.DiscoveryFeature;

    public DiscoveryFeature? DiscoveryFeature { get; private set; }

    public int? DriverVersion { get; private set; }

    public FavoritesFeature? FavoritesFeature { get; private set; }

    IFavoritesFeature? IDeviceBuilder.FavoritesFeature => this.FavoritesFeature;

    public bool HasPowerStateSensor => this.Sensors.ContainsKey(Constants.PowerSensorName);

    public DeviceIconOverride? Icon { get; private set; }

    public Dictionary<string, ImageUrlConstruct> ImageUrls { get; } = new();

    IReadOnlyCollection<string> IDeviceBuilder.ImageUrls => this.ImageUrls.Keys;

    public DeviceInitializer? Initializer { get; private set; }

    public string Manufacturer { get; private set; } = "NEEO";

    public string Name { get; }

    public DeviceNotifierCallback? NotifierCallback { get; private set; }

    IRegistrationFeature? IDeviceBuilder.RegistrationFeature => this.RegistrationFeature;

    public RegistrationFeature? RegistrationFeature { get; private set; }

    public Dictionary<string, SensorConstruct> Sensors { get; } = new();

    IReadOnlyCollection<string> IDeviceBuilder.Sensors => this.Sensors.Keys;

    public DeviceSetup Setup { get; } = new DeviceSetup();

    public Dictionary<string, SliderConstruct> Sliders { get; } = new();

    IReadOnlyCollection<string> IDeviceBuilder.Sliders => this.Sliders.Keys;

    public string? SpecificName { get; private set; }

    public SubscriptionFeature? SubscriptionFeature { get; private set; }

    ISubscriptionFeature? IDeviceBuilder.SubscriptionFeature => this.SubscriptionFeature;

    public Dictionary<string, SwitchConstruct> Switches { get; } = new();

    IReadOnlyCollection<string> IDeviceBuilder.Switches => this.Switches.Keys;

    public Dictionary<string, TextLabelConstruct> TextLabels { get; } = new();

    IReadOnlyCollection<string> IDeviceBuilder.TextLabels => this.TextLabels.Keys;

    public DeviceTiming? Timing { get; private set; }

    public DeviceType Type { get; }

    IDeviceBuilder IDeviceBuilder.AddAdditionalSearchTokens(IEnumerable<string> tokens) => this.AddAdditionalSearchTokens(tokens);

    IDeviceBuilder IDeviceBuilder.AddButton(string name, string? label) => this.AddButton(name, label);

    IDeviceBuilder IDeviceBuilder.AddButton(KnownButtons button) => this.AddButton(button);

    IDeviceBuilder IDeviceBuilder.AddButtonGroup(ButtonGroups group) => this.AddButtonGroup(group);

    IDeviceBuilder IDeviceBuilder.AddButtonHandler(ButtonHandler handler) => this.AddButtonHandler(handler);

    IDeviceBuilder IDeviceBuilder.AddCharacteristic(DeviceCharacteristic characteristic) => this.AddCharacteristic(characteristic);

    IDeviceBuilder IDeviceBuilder.AddDirectory(
        string name,
        string? label,
        DirectoryRole? role,
        DeviceDirectoryPopulator populator,
        DirectoryActionHandler actionHandler
    ) => this.AddDirectory(name, label, role, populator, actionHandler);

    IDeviceBuilder IDeviceBuilder.AddFavoriteHandler(FavoriteHandler handler) => this.AddFavoriteHandler(handler);

    IDeviceBuilder IDeviceBuilder.AddImageUrl(
            string name,
        string? label,
        ImageSize size,
        string? uri,
        DeviceValueGetter<string>? getter
    ) => this.AddImageUrl(name, label, size, uri, getter);

    IDeviceBuilder IDeviceBuilder.AddPlayerWidget(IPlayerWidgetCallbacks callbacks) => this.AddPlayerWidget(callbacks);

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

    IDeviceAdapter IDeviceBuilder.BuildAdapter() => this.BuildAdapter();

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
        DeviceSubscriptionHandler onDeviceAdded,
        DeviceSubscriptionHandler onDeviceRemoved,
        DeviceSubscriptionListHandler initializeDeviceList
    ) => this.RegisterDeviceSubscriptionCallbacks(onDeviceAdded, onDeviceRemoved, initializeDeviceList);

    IDeviceBuilder IDeviceBuilder.RegisterInitializer(DeviceInitializer initializer) => this.RegisterInitializer(initializer);

    IDeviceBuilder IDeviceBuilder.SetDriverVersion(int? version) => this.SetDriverVersion(version);

    IDeviceBuilder IDeviceBuilder.SetIcon(DeviceIconOverride icon) => this.SetIcon(icon);

    IDeviceBuilder IDeviceBuilder.SetManufacturer(string manufacturer) => this.SetManufacturer(manufacturer);

    IDeviceBuilder IDeviceBuilder.SetSpecificName(string? specificName) => this.SetSpecificName(specificName);

    private DeviceBuilder AddAdditionalSearchTokens(IEnumerable<string> tokens)
    {
        this._additionalSearchTokens.AddRange(tokens ?? throw new ArgumentNullException(nameof(tokens)));
        return this;
    }

    private DeviceBuilder AddButton(string name, string? label = default)
    {
        ButtonConstruct definition = new(
            Validator.ValidateText(name),
            Validator.ValidateText(label, allowNull: true)
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

    private DeviceBuilder AddButton(KnownButtons buttons) => KnownButton.GetNames(buttons).Aggregate(
        this,
        static (builder, name) => builder.AddButton(name)
    );

    private DeviceBuilder AddButtonGroup(ButtonGroups group) => this.AddButton((KnownButtons)group);

    private DeviceBuilder AddButtonHandler(ButtonHandler handler)
    {
        if (this.ButtonHandler != null)
        {
            throw new InvalidOperationException("ButtonHandler already defined.");
        }
        this.ButtonHandler = handler ?? throw new ArgumentNullException(nameof(handler));
        return this;
    }

    private DeviceBuilder AddCharacteristic(DeviceCharacteristic characteristic)
    {
        this._characteristics.Add(characteristic);
        return this;
    }

    private DeviceBuilder AddDirectory(string name, string? label, DirectoryRole? role, DeviceDirectoryPopulator populator, DirectoryActionHandler actionHandler)
    {
        if (role.HasValue && Interlocked.Exchange(ref this._roles, this._roles | (int)role.Value) == this._roles)
        {
            throw new InvalidOperationException($"Directory with role {role} already defined.");
        }
        DirectoryConstruct definition = new(
           Validator.ValidateText(name),
           Validator.ValidateText(label, allowNull: true),
           role,
           new(populator ?? throw new ArgumentNullException(nameof(populator)), actionHandler ?? throw new ArgumentNullException(nameof(actionHandler)))
        );
        if (!this.Directories.TryAdd(name, definition))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        return this;
    }

    private DeviceBuilder AddFavoriteHandler(FavoriteHandler handler)
    {
        if (this.FavoritesFeature != null)
        {
            throw new InvalidOperationException($"{nameof(FavoriteHandler)} already defined.");
        }
        if (!this.Type.SupportsFavorites())
        {
            throw new NotSupportedException($"Device type {this.Type} does not support favorites.");
        }
        this.FavoritesFeature = new(handler);
        return this;
    }

    private DeviceBuilder AddImageUrl(string name, string? label, ImageSize size, string? uri, DeviceValueGetter<string>? getter)
    {
        ImageUrlConstruct definition = new(
            Validator.ValidateText(name),
            Validator.ValidateText(label, allowNull: true),
            ValueFeature.Create((getter, uri) switch
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

    private DeviceBuilder AddPlayerWidget(IPlayerWidgetCallbacks callbacks)
    {
        if (this.Type.SupportsPlayerWidget())
        {
            throw new InvalidOperationException($"{this.Type} does not support the player widget.");
        }
        if (this._hasPlayerWidget)
        {
            throw new InvalidOperationException("Player widget already defined.");
        }
        this._hasPlayerWidget = true;
        if ((callbacks ?? throw new ArgumentNullException(nameof(callbacks))) is IQueueingPlayerWidgetCallbacks queueCallbacks)
        {
            this.AddDirectory("QUEUE_DIRECTORY", "QUEUE", DirectoryRole.Queue, queueCallbacks.PopulateQueueDirectoryAsync, queueCallbacks.HandleQueueDirectoryActionAsync);
        }
        return this
           .AddButtonGroup(ButtonGroups.Volume)
           .AddButton(KnownButtons.Play | KnownButtons.PlayToggle | KnownButtons.Pause | KnownButtons.NextTrack | KnownButtons.PreviousTrack | KnownButtons.ShuffleToggle | KnownButtons.RepeatToggle | KnownButtons.ClearQueue)
           .AddSlider("VOLUME", null, callbacks.GetVolumeAsync, callbacks.SetVolumeAsync, 0d, 100d, "%")
           .AddDirectory("ROOT_DIRECTORY", "ROOT", DirectoryRole.Root, callbacks.PopulateRootDirectoryAsync, callbacks.HandleRootDirectoryActionAsync)
           .AddSensor("COVER_ART_SENSOR", null, callbacks.GetCoverArtUriAsync)
           .AddSensor("TITLE_SENSOR", null, callbacks.GetTitleAsync)
           .AddSensor("DESCRIPTION_SENSOR", null, callbacks.GetDescriptionAsync)
           .AddSwitch("PLAYING", null, callbacks.GetIsPlayingAsync, callbacks.SetIsPlayingAsync)
           .AddSwitch("SHUFFLE", null, callbacks.GetShuffleAsync, callbacks.SetShuffleAsync)
           .AddSwitch("REPEAT", null, callbacks.GetRepeatAsync, callbacks.SetRepeatAsync);
    }

    private DeviceBuilder AddPowerStateSensor(DeviceValueGetter<bool> getter)
    {
        SensorConstruct definition = new(
            SensorType.Power,
            Constants.PowerSensorName,
            Constants.PowerSensorLabel,
            ValueFeature.Create(getter)
        );
        if (!this.Sensors.TryAdd(Constants.PowerSensorName, definition))
        {
            throw new InvalidOperationException("PowerState sensor already added.");
        }
        return this;
    }

    private DeviceBuilder AddSensor(SensorType type, string name, string? label, ValueFeature controller)
    {
        if (name == Constants.PowerSensorName)
        {
            throw new ArgumentException($"Name can not be {Constants.PowerSensorName}.", nameof(name));
        }
        SensorConstruct definition = new(
            type,
            Validator.ValidateText(name),
            Validator.ValidateText(label, allowNull: true),
            controller
        );
        if (!this.Sensors.TryAdd(name, definition))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        return this;
    }

    private DeviceBuilder AddSensor(string name, string? label, DeviceValueGetter<bool> getter) => this.AddSensor(SensorType.Binary, name, label, ValueFeature.Create(getter));

    private DeviceBuilder AddSensor(string name, string? label, DeviceValueGetter<string> getter) => this.AddSensor(SensorType.String, name, label, ValueFeature.Create(getter));

    private DeviceBuilder AddSensor(string name, string? label, DeviceValueGetter<double> getter, double rangeLow, double rangeHigh, string unit)
    {
        if (name == Constants.PowerSensorName)
        {
            throw new ArgumentException($"Name can not be {Constants.PowerSensorName}.", nameof(name));
        }
        RangeSensorConstruct definition = new(
            Validator.ValidateText(name),
            Validator.ValidateText(label, allowNull: true),
            ValueFeature.Create(getter),
            Validator.ValidateRange(rangeLow, rangeHigh),
            Validator.ValidateText(unit)
        );
        if (!this.Sensors.TryAdd(name, definition))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        return this;
    }

    private DeviceBuilder AddSlider(string name, string? label, DeviceValueGetter<double> getter, DeviceValueSetter<double> setter, double rangeLow, double rangeHigh, string unit)
    {
        SliderConstruct definition = new(
            Validator.ValidateText(name),
            Validator.ValidateText(label, allowNull: true),
            ValueFeature.Create(getter, setter),
            Validator.ValidateRange(rangeLow, rangeHigh),
            Validator.ValidateText(unit)
        );
        if (!this.Sliders.TryAdd(name, definition))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        return this;
    }

    private DeviceBuilder AddSwitch(string name, string? label, DeviceValueGetter<bool> getter, DeviceValueSetter<bool> setter)
    {
        SwitchConstruct definition = new(
            Validator.ValidateText(name),
            Validator.ValidateText(label, allowNull: true),
            ValueFeature.Create(getter, setter)
        );
        if (!this.Switches.TryAdd(name, definition))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        return this;
    }

    private DeviceBuilder AddTextLabel(string name, string? label, bool? isLabelVisible, DeviceValueGetter<string> getter)
    {
        TextLabelConstruct definition = new(
            Validator.ValidateText(name),
            Validator.ValidateText(label, allowNull: true),
            ValueFeature.Create(getter),
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
        List<Component> components = new();
        Dictionary<string, IFeature> features = new();
        foreach ((string name, string? label) in this.Buttons.Values)
        {
            AddComponentAndRouteHandler(BuildButton(pathPrefix, name, label), new ButtonFeature(this.ButtonHandler!, name));
        }
        foreach ((string name, string? label, ValueFeature valueFeature, IReadOnlyCollection<double> range, string unit) in this.Sliders.Values)
        {
            AddComponentAndRouteHandler(BuildSensor(pathPrefix, name, label, new RangeSensorDetails(range, unit)), valueFeature);
            AddComponentAndRouteHandler(BuildSlider(pathPrefix, name, label, range, unit), valueFeature);
        }
        foreach ((string name, string? label, ValueFeature valueFeature) in this.Switches.Values)
        {
            AddComponentAndRouteHandler(BuildSensor(pathPrefix, name, label, new(SensorType.Binary)), valueFeature);
            AddComponentAndRouteHandler(BuildSwitch(pathPrefix, name, label), valueFeature);
        }
        foreach ((string name, string? label, ValueFeature valueFeature, bool? isLabelVisible) in this.TextLabels.Values)
        {
            AddComponentAndRouteHandler(BuildSensor(pathPrefix, name, label, new(SensorType.String)), valueFeature);
            AddComponentAndRouteHandler(BuildTextLabel(pathPrefix, name, label, isLabelVisible), valueFeature);
        }
        foreach ((string name, string? label, ValueFeature valueFeature, ImageSize size, string? uri) in this.ImageUrls.Values)
        {
            AddComponentAndRouteHandler(BuildSensor(pathPrefix, name, label, new(SensorType.String)), valueFeature);
            AddComponentAndRouteHandler(BuildImageUrl(pathPrefix, name, label, size, uri), valueFeature);
        }
        foreach (DirectoryConstruct definition in this.Directories.Values)
        {
            AddComponentAndRouteHandler(BuildDirectory(pathPrefix, definition), definition.Feature);
        }
        foreach (SensorConstruct definition in this.Sensors.Values)
        {
            AddComponentAndRouteHandler(
                definition switch
                {
                    RangeSensorConstruct rsd => BuildSensor(pathPrefix, rsd.Name, rsd.Label, new RangeSensorDetails(rsd.Range, rsd.Unit)),
                    { Type: SensorType.Power } => BuildPowerSensor(pathPrefix),
                    _ => BuildSensor(pathPrefix, definition.Name, definition.Label, new(definition.Type))
                },
                definition.ValueFeature
            );
        }
        if (this.DiscoveryFeature is { } discoveryFeature)
        {
            AddRouteHandler(BuildComponent(pathPrefix, ComponentType.Discovery), discoveryFeature);
            if (this.RegistrationFeature is { } registrationFeature)
            {
                deviceCapabilities.Add(DeviceCapability.RegisterUserAccount);
                AddRouteHandler(BuildComponent(pathPrefix, ComponentType.Registration), registrationFeature);
            }
        }
        else if (!deviceCapabilities.Contains(DeviceCapability.DynamicDevice) && deviceCapabilities.FindIndex(RequiresDiscovery) is int index and > -1)
        {
            throw new InvalidOperationException($"Discovery required for {deviceCapabilities[index]}.");
        }
        if (this.FavoritesFeature is { } favoritesFeature)
        {
            if (this._digitCount != 10)
            {
                throw new InvalidOperationException("Can not enable favorites without the 10 digit buttons being added. It is highly recommended to call AddButtonGroup(ButtonGroup.NumberPad).");
            }
            deviceCapabilities.Add(DeviceCapability.CustomFavoriteHandler);
            AddRouteHandler(BuildComponent(pathPrefix, ComponentType.FavoritesHandler), favoritesFeature);
        }
        if (this.SubscriptionFeature is { } subscriptionFeature)
        {
            AddRouteHandler(BuildComponent(pathPrefix, ComponentType.Subscription), subscriptionFeature);
        }
        return new(
            this.AdapterName,
            components,
            features,
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

        void AddComponentAndRouteHandler(Component component, IFeature feature)
        {
            AddRouteHandler(component, feature);
            components.Add(component);
        }

        void AddRouteHandler(Component component, IFeature feature)
        {
            features[Uri.UnescapeDataString(component.Name)] = paths.Add(component.Path)
                ? feature
                : throw new InvalidOperationException($"Path {component.Path} is not unique.");
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

        static DirectoryComponent BuildDirectory(string pathPrefix, DirectoryConstruct definition)
        {
            (string featureName, string? label, DirectoryRole? role, _) = definition;
            string name = Uri.EscapeDataString(featureName);
            string path = pathPrefix + name;
            return new(
                name,
                label is { } text ? Uri.EscapeDataString(text) : name,
                path,
                role
            );
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
            SensorComponent component = BuildSensor(pathPrefix, "-", Constants.PowerSensorLabel, new(SensorType.Power));
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
        this.Setup.DiscoveryHeaderText = Validator.ValidateText(headerText, maxLength: 255);
        this.Setup.DiscoveryDescription = Validator.ValidateText(description, maxLength: 255);
        this.Setup.Discovery = true;
        this.Setup.EnableDynamicDeviceBuilder = enableDynamicDeviceBuilder;
        this.DiscoveryFeature = new DiscoveryFeature(process, enableDynamicDeviceBuilder);
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
        this.Setup.RegistrationHeaderText = Validator.ValidateText(headerText, maxLength: 255);
        this.Setup.RegistrationDescription = Validator.ValidateText(description, maxLength: 255);
        this.Setup.RegistrationType = type;
        this.RegistrationFeature = new(queryIsRegistered, type, element => processor(element.Deserialize<TPayload>(JsonSerialization.Options)!));
        return this;
    }

    private DeviceBuilder RegisterDeviceSubscriptionCallbacks(DeviceSubscriptionHandler onDeviceAdded, DeviceSubscriptionHandler onDeviceRemoved, DeviceSubscriptionListHandler initializeDeviceList)
    {
        if (this.SubscriptionFeature != null)
        {
            throw new InvalidOperationException($"{nameof(this.RegisterDeviceSubscriptionCallbacks)} was already called.");
        }
        this.SubscriptionFeature = new(
            onDeviceAdded ?? throw new ArgumentNullException(nameof(onDeviceAdded)),
            onDeviceRemoved ?? throw new ArgumentNullException(nameof(onDeviceRemoved)),
            initializeDeviceList ?? throw new ArgumentNullException(nameof(initializeDeviceList))
        );
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

    private DeviceBuilder SetDriverVersion(int? version)
    {
        this.DriverVersion = Validator.ValidateNotNegative(version);
        return this;
    }

    private DeviceBuilder SetIcon(DeviceIconOverride icon)
    {
        this.Icon = icon;
        return this;
    }

    private DeviceBuilder SetManufacturer(string manufacturer)
    {
        Validator.ValidateText(this.Manufacturer = manufacturer);
        return this;
    }

    private DeviceBuilder SetSpecificName(string? specificName)
    {
        Validator.ValidateText(this.SpecificName = specificName, allowNull: true);
        return this;
    }

    private sealed record class SecurityCodeContainer(String SecurityCode);

    public abstract record Construct(string Name, string? Label);

    public sealed record ButtonConstruct(string Name, string? Label) : Construct(Name, Label);

    public sealed record DirectoryConstruct(string Name, string? Label, DirectoryRole? Role, DirectoryFeature Feature) : Construct(Name, Label);

    public sealed record ImageUrlConstruct(string Name, string? Label, ValueFeature Feature, ImageSize Size, String? Uri) : Construct(Name, Label);

    public sealed record TextLabelConstruct(string Name, string? Label, ValueFeature Feature, bool? IsLabelVisible) : Construct(Name, Label);

    public record SensorConstruct(SensorType Type, string Name, string? Label, ValueFeature ValueFeature) : Construct(Name, Label);

    public sealed record RangeSensorConstruct(string Name, string? Label, ValueFeature Feature, IReadOnlyCollection<double> Range, string Unit) : SensorConstruct(SensorType.Range, Name, Label, Feature);

    public sealed record SliderConstruct(string Name, string? Label, ValueFeature Feature, IReadOnlyCollection<double> Range, string Unit) : Construct(Name, Label);

    public sealed record SwitchConstruct(string Name, string? Label, ValueFeature Feature) : Construct(Name, Label);
}