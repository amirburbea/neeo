using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Components;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Devices.Features;
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
    /// Gets the names of the buttons defined for the device.
    /// </summary>
    IReadOnlyCollection<string> Buttons { get; }

    /// <summary>
    /// Gets the collection of special characteristics of the device.
    /// </summary>
    IReadOnlyCollection<DeviceCharacteristic> Characteristics { get; }

    /// <summary>
    /// Gets the names of the directories defined for the device.
    /// </summary>
    IReadOnlyCollection<string> Directories { get; }

    /// <summary>
    /// Gets the discovery feature (which will be <see langword="null"/> until the device is configured to support discovery via
    /// a call to <see cref="IDeviceBuilder.EnableDiscovery"/>).
    /// </summary>
    IDiscoveryFeature? DiscoveryFeature { get; }

    /// <summary>
    /// Version of the device driver.
    /// <para />
    /// Incrementing this version will cause the brain to query for new components.
    /// </summary>
    int? DriverVersion { get; }

    /// <summary>
    /// Gets the favorites feature (which will be <see langword="null"/> until the device is configured to support custom favorites
    /// via a call to <see cref="IDeviceBuilder.AddFavoriteHandler"/>).
    /// </summary>
    IFavoritesFeature? FavoritesFeature { get; }

    /// <summary>
    /// Gets a value indicating if a power state sensor has been defined for the device.
    /// </summary>
    bool HasPowerStateSensor { get; }

    /// <summary>
    /// Gets the device icon override, if <see langword="null"/> a default is selected depending on the device type.
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

    /// <summary>
    /// Gets the device notifier callback (which will be <see langword="null"/> until the device is configured to support notifications
    /// via a call to <see cref="IDeviceBuilder.EnableNotifications"/>).
    /// </summary>
    DeviceNotifierCallback? NotifierCallback { get; }

    /// <summary>
    /// Gets the registration feature (which will be <see langword="null"/> until the device is configured to support discovery and registration
    /// via a call to <see cref="IDeviceBuilder" />.EnableRegistration).
    /// </summary>
    IRegistrationFeature? RegistrationFeature { get; }

    /// <summary>
    /// Gets the names of the sensors defined for the device.
    /// </summary>
    IReadOnlyCollection<string> Sensors { get; }

    /// <summary>
    /// Gets information about the current device's setup information (namely registration and discovery).
    /// </summary>
    DeviceSetup Setup { get; }

    /// <summary>
    /// Gets the names of the sliders defined for the device.
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

    /// <summary>
    /// Adds one or more tokens to help find this device in search.
    /// <para />
    /// Note that manufacturer, type, and device name are already included by default as search tokens.
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
    IDeviceBuilder AddAdditionalSearchTokens(params string[] tokens) => this.AddAdditionalSearchTokens((IEnumerable<string>)tokens);

    /// <summary>
    /// Adds one or more tokens to help find this device in search.
    /// <para />
    /// Note that manufacturer, type, and device name are already included by default as search tokens.
    /// </summary>
    /// <param name="tokens"></param>
    /// <returns></returns>
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
    IDeviceBuilder AddButton(Buttons buttons);

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

    /// <summary>
    /// Adds a special/unique characteristic to the device definition.
    /// </summary>
    /// <param name="characteristic">The characteristic to add to the device definition.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    IDeviceBuilder AddCharacteristic(DeviceCharacteristic characteristic);

    IDeviceBuilder AddDirectory(string name, string? label, DirectoryRole? role, DirectoryBrowser populator, DirectoryActionHandler actionHandler, string? identifier = default);

    /// <summary>
    /// Sets a callback to be invoked in response to calls from the NEEO Brain to handle launching favorites.
    /// </summary>
    /// <param name="handler">The favorites handler callback.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    IDeviceBuilder AddFavoriteHandler(FavoriteHandler handler);

    IDeviceBuilder AddImageUrl(string name, string? label, ImageSize size, DeviceValueGetter<string>? getter = default, string? uri = default);

    IDeviceBuilder AddPlayerWidget(IPlayerWidgetController controller);

    /// <summary>
    /// Defines a sensor by which NEEO can detemine if the device is powered on/off. This is useful in
    /// situations where otherwise NEEO may have labeled the device &quot;stupid&quot;.
    /// 
    /// Additionally, if the device has notification support (via a call to <see cref="IDeviceBuilder.EnableNotifications"/>),
    /// this enables the use of the <see cref="IDeviceNotifier.SendPowerNotificationAsync"/> method.
    /// </summary>
    /// <param name="sensor">A sensor callback that can be used to determine if the device is on or off.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    IDeviceBuilder AddPowerStateSensor(DeviceValueGetter<bool> sensor);

    IDeviceBuilder AddSensor(string name, string? label, DeviceValueGetter<double> getter, double rangeLow = 0d, double rangeHigh = 100d, string unit = "%");

    IDeviceBuilder AddSensor(string name, string? label, DeviceValueGetter<bool> getter);

    IDeviceBuilder AddSensor(string name, string? label, DeviceValueGetter<string> getter);

    IDeviceBuilder AddSensor(string name, string? label, DeviceValueGetter<object> getter);

    IDeviceBuilder AddSlider(string name, string? label, DeviceValueGetter<double> getter, DeviceValueSetter<double> setter, double rangeLow = 0d, double rangeHigh = 100d, string unit = "%");

    IDeviceBuilder AddSmartApplicationButton(SmartApplicationButtons button);

    IDeviceBuilder AddSwitch(string name, string? label, DeviceValueGetter<bool> getter, DeviceValueSetter<bool> setter);

    IDeviceBuilder AddTextLabel(string name, string? label, DeviceValueGetter<string> getter, bool? isLabelVisible = default);

    /// <summary>
    /// Builds a device adapter based on this instance.
    /// </summary>
    /// <returns>The created device adapter.</returns>
    IDeviceAdapter BuildAdapter();

    /// <summary>
    /// Set timing related information (the delays NEEO should use when interacting with a device),
    /// which will be used when generating recipes.
    /// </summary>
    /// <param name="powerOnDelay">
    /// Optional: The number of milliseconds NEEO should wait after powering on the device
    /// before sending it another command.
    /// </param>
    /// <param name="sourceSwitchDelay">
    /// Optional: The number of milliseconds NEEO should wait after switching input on the device
    /// before sending it another command.
    /// </param>
    /// <param name="shutdownDelay">
    /// Optional: The number of milliseconds NEEO should wait after shutting down the device
    /// before sending it another command.
    /// </param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    IDeviceBuilder DefineTiming(int? powerOnDelay = default, int? shutdownDelay = default, int? sourceSwitchDelay = default);

    /// <summary>
    /// Enables the device adapter to take advantage of the HTTP server set up for the Brain integration. Enabling
    /// device routes will invoke the <paramref name="routeHandler"/> callback to handle all HTTP requests with a URI 
    /// starting with a specific prefix. This prefix is supplied to the device adapter via the <paramref name="uriPrefixCallback"/>.
    /// <para/>
    /// This could be used for serving images to the remote, or when integration with the underlying device itself requires an HTTP server.
    /// </summary>
    /// <param name="uriPrefixCallback">Callback invoked upon starting the REST server indicating which prefix to use for requests to be handled by this device.</param>
    /// <param name="routeHandler">Callback to handle HTTP requests with the URI prefix.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    IDeviceBuilder EnableDeviceRoute(UriPrefixCallback uriPrefixCallback, DeviceRouteHandler routeHandler);

    IDeviceBuilder EnableDiscovery(string headerText, string description, DiscoveryProcess process, bool enableDynamicDeviceBuilder = false);

    /// <summary>
    /// Specify that this device will be able to send notifications to the NEEO Brain about changes in the state of one or more of its components.
    /// A callback is provided for receiving an <see cref="IDeviceNotifier"/>.
    /// </summary>
    /// <param name="callback">A callback for receiving the <see cref="IDeviceNotifier"/>.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    IDeviceBuilder EnableNotifications(DeviceNotifierCallback callback);

    /// <summary>
    /// Instructs NEEO during setup of a device previously configured to support discovery (via a call to <see cref="IDeviceBuilder.EnableDiscovery"/>),
    /// to prompt the user for a user name and password for devices requiring credentials.
    /// </summary>
    /// <param name="headerText">The header text to display when a user is entering registration credentials.</param>
    /// <param name="description">The descriptive summary text to display when a user is entering registration credentials.</param>
    /// <param name="queryIsRegistered">
    /// A callback invoked by the NEEO Brain to check whether registration has been previously performed successfully.
    /// <para />
    /// If the task result is <see langword = "true" /> then the setup process will skip registration.
    /// </param>
    /// <param name="processor">Callback to process the credentials and return a <see cref="RegistrationResult"/>.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
    IDeviceBuilder EnableRegistration(string headerText, string description, QueryIsRegistered queryIsRegistered, CredentialsRegistrationProcessor processor);

    /// <summary>
    /// Instructs NEEO during setup of a device previously configured to support discovery (via a call to <see cref="IDeviceBuilder.EnableDiscovery"/>),
    /// to prompt the user for a security code for devices requiring credentials.
    /// </summary>
    /// <param name="headerText">The header text to display when a user is entering registration credentials.</param>
    /// <param name="description">The descriptive summary text to display when a user is entering registration credentials.</param>
    /// <param name="queryIsRegistered">
    /// A callback invoked by the NEEO Brain to check whether registration has been previously performed successfully.
    /// <para />
    /// If the task result is <see langword = "true" /> then the setup process will skip registration.
    /// </param>
    /// <param name="processor">Callback to process the security code and return a <see cref="RegistrationResult"/>.</param>
    /// <returns><see cref="IDeviceBuilder"/> for chaining.</returns>
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
    private readonly Dictionary<string, ButtonParameters> _buttons = new();
    private readonly HashSet<DeviceCharacteristic> _characteristics = new();
    private readonly Dictionary<string, DirectoryParameters> _directories = new();
    private readonly Dictionary<string, ImageUrlParameters> _imageUrls = new();
    private readonly Dictionary<string, SensorParameters> _sensors = new();
    private readonly Dictionary<string, SliderParameters> _sliders = new();
    private readonly Dictionary<string, SwitchParameters> _switches = new();
    private readonly Dictionary<string, TextLabelParameters> _textLabels = new();
    private int _digitCount;
    private bool _hasInput;
    private bool _hasPlayerWidget;
    private int _roles;
    private DeviceRouteHandler? _routeHandler;
    private UriPrefixCallback? _uriPrefixCallback;

    internal DeviceBuilder(string name, DeviceType type, string? prefix)
    {
        (this.Type, this.Name) = (type, Validator.ValidateText(name));
        this.AdapterName = $"apt-{UniqueNameGenerator.Generate(name, prefix)}";
    }

    public string AdapterName { get; }

    public IReadOnlyCollection<string> AdditionalSearchTokens => this._additionalSearchTokens;

    public ButtonHandler? ButtonHandler { get; private set; }

    IReadOnlyCollection<string> IDeviceBuilder.Buttons => this._buttons.Keys;

    public IReadOnlyCollection<DeviceCharacteristic> Characteristics => this._characteristics;

    IReadOnlyCollection<string> IDeviceBuilder.Directories => this._directories.Keys;

    IDiscoveryFeature? IDeviceBuilder.DiscoveryFeature => this.DiscoveryFeature;

    public DiscoveryFeature? DiscoveryFeature { get; private set; }

    public int? DriverVersion { get; private set; }

    public FavoritesFeature? FavoritesFeature { get; private set; }

    IFavoritesFeature? IDeviceBuilder.FavoritesFeature => this.FavoritesFeature;

    public bool HasPowerStateSensor => this._sensors.ContainsKey(Constants.PowerSensorName);

    public DeviceIconOverride? Icon { get; private set; }

    IReadOnlyCollection<string> IDeviceBuilder.ImageUrls => this._imageUrls.Keys;

    public DeviceInitializer? Initializer { get; private set; }

    public string Manufacturer { get; private set; } = "NEEO";

    public string Name { get; }

    public DeviceNotifierCallback? NotifierCallback { get; private set; }

    IRegistrationFeature? IDeviceBuilder.RegistrationFeature => this.RegistrationFeature;

    public RegistrationFeature? RegistrationFeature { get; private set; }

    IReadOnlyCollection<string> IDeviceBuilder.Sensors => this._sensors.Keys;

    public DeviceSetup Setup { get; } = new DeviceSetup();

    IReadOnlyCollection<string> IDeviceBuilder.Sliders => this._sliders.Keys;

    public string? SpecificName { get; private set; }

    public SubscriptionFeature? SubscriptionFeature { get; private set; }

    ISubscriptionFeature? IDeviceBuilder.SubscriptionFeature => this.SubscriptionFeature;

    IReadOnlyCollection<string> IDeviceBuilder.Switches => this._switches.Keys;

    IReadOnlyCollection<string> IDeviceBuilder.TextLabels => this._textLabels.Keys;

    public DeviceTiming? Timing { get; private set; }

    public DeviceType Type { get; }

    IDeviceBuilder IDeviceBuilder.AddAdditionalSearchTokens(IEnumerable<string> tokens) => this.AddAdditionalSearchTokens(tokens);

    IDeviceBuilder IDeviceBuilder.AddButton(string name, string? label) => this.AddButton(name, label);

    IDeviceBuilder IDeviceBuilder.AddButton(Buttons button) => this.AddButton(button);

    IDeviceBuilder IDeviceBuilder.AddButtonGroup(ButtonGroups group) => this.AddButtonGroup(group);

    IDeviceBuilder IDeviceBuilder.AddButtonHandler(ButtonHandler handler) => this.AddButtonHandler(handler);

    IDeviceBuilder IDeviceBuilder.AddCharacteristic(DeviceCharacteristic characteristic) => this.AddCharacteristic(characteristic);

    IDeviceBuilder IDeviceBuilder.AddDirectory(
        string name,
        string? label,
        DirectoryRole? role,
        DirectoryBrowser populator,
        DirectoryActionHandler actionHandler,
        string? identifier
    ) => this.AddDirectory(name, label, role, populator, actionHandler, identifier);

    IDeviceBuilder IDeviceBuilder.AddFavoriteHandler(FavoriteHandler handler) => this.AddFavoriteHandler(handler);

    IDeviceBuilder IDeviceBuilder.AddImageUrl(
        string name,
        string? label,
        ImageSize size,
        DeviceValueGetter<string>? getter,
        string? uri
    ) => this.AddImageUrl(name, label, size, getter, uri);

    IDeviceBuilder IDeviceBuilder.AddPlayerWidget(IPlayerWidgetController controller) => this.AddPlayerWidget(controller);

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

    IDeviceBuilder IDeviceBuilder.AddSensor(
        string name,
        string? label,
        DeviceValueGetter<object> getter
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

    IDeviceBuilder IDeviceBuilder.AddSmartApplicationButton(SmartApplicationButtons button) => this.AddSmartApplicationButton(button);

    IDeviceBuilder IDeviceBuilder.AddSwitch(
        string name,
        string? label,
        DeviceValueGetter<bool> getter,
        DeviceValueSetter<bool> setter
    ) => this.AddSwitch(name, label, getter, setter);

    IDeviceBuilder IDeviceBuilder.AddTextLabel(
       string name,
       string? label,
       DeviceValueGetter<string> getter,
       bool? isLabelVisible) => this.AddTextLabel(name, label, getter, isLabelVisible);

    IDeviceAdapter IDeviceBuilder.BuildAdapter() => this.BuildAdapter();

    IDeviceBuilder IDeviceBuilder.DefineTiming(
        int? powerOnDelay,
        int? shutdownDelay,
        int? sourceSwitchDelay
    ) => this.DefineTiming(powerOnDelay, shutdownDelay, sourceSwitchDelay);

    IDeviceBuilder IDeviceBuilder.EnableDeviceRoute(UriPrefixCallback uriPrefixCallback, DeviceRouteHandler routeHandler) => this.EnableDeviceRoute(uriPrefixCallback, routeHandler);

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
        (Credentials credentials) => processor(credentials.UserName, credentials.Password)
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
        ButtonParameters parameters = new(
            Validator.ValidateText(name),
            Validator.ValidateText(label, allowNull: true)
        );
        if (!this._buttons.TryAdd(name, parameters))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        this._hasInput |= name.StartsWith(DeviceBuilderConstants.InputPrefix);
        this._digitCount += DeviceBuilder._digitRegex.IsMatch(name) ? 1 : 0;
        return this;
    }

    private DeviceBuilder AddButton(Buttons buttons) => Button.GetNames(buttons).Aggregate(
        this,
        static (builder, name) => builder.AddButton(name)
    );

    private DeviceBuilder AddButtonGroup(ButtonGroups group) => this.AddButton((Buttons)group);

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

    private DeviceBuilder AddDirectory(string name, string? label, DirectoryRole? role, DirectoryBrowser browser, DirectoryActionHandler actionHandler, string? identifier = default)
    {
        if (role.HasValue && Interlocked.Exchange(ref this._roles, this._roles | (int)role.Value) == this._roles)
        {
            throw new InvalidOperationException($"Directory with role {role} already defined.");
        }
        DirectoryParameters parameters = new(
           Validator.ValidateText(name),
           Validator.ValidateText(label, allowNull: true),
           role,
           new(browser, actionHandler, identifier)
        );
        if (!this._directories.TryAdd(name, parameters))
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

    private DeviceBuilder AddImageUrl(string name, string? label, ImageSize size, DeviceValueGetter<string>? getter, string? uri)
    {
        ImageUrlParameters parameters = new(
            Validator.ValidateText(name),
            Validator.ValidateText(label, allowNull: true),
            ValueFeature.Create((getter, uri) switch
            {
                (null, null) => throw new InvalidOperationException($"Either {nameof(uri)} or {nameof(getter)} must be specified."),
                (null, { }) => _ => Task.FromResult(uri),
                ({ }, _) => getter
            }),
            size,
            uri
        );
        if (!this._imageUrls.TryAdd(name, parameters))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        return this;
    }

    private DeviceBuilder AddPlayerWidget(IPlayerWidgetController controller)
    {
        if (!this.Type.SupportsPlayerWidget())
        {
            throw new InvalidOperationException($"{this.Type} does not support the player widget.");
        }
        if (this._hasPlayerWidget)
        {
            throw new InvalidOperationException("Player widget already defined.");
        }
        this._hasPlayerWidget = true;
        if (controller.IsQueueSupported)
        {
            this.AddDirectory(PlayerWidgetConstants.QueueDirectoryName, controller.QueueDirectoryLabel ?? "Queue", DirectoryRole.Queue, controller.PopulateQueueDirectoryAsync, controller.HandleQueueDirectoryActionAsync);
        }
        return this
           .AddButton(PlayerWidgetConstants.PlayerButtons)
           .AddSlider(PlayerWidgetConstants.VolumeSliderName, null, controller.GetVolumeAsync, controller.SetVolumeAsync, 0d, 100d, "%")
           .AddDirectory(PlayerWidgetConstants.RootDirectoryName, controller.RootDirectoryLabel ?? "Library", DirectoryRole.Root, controller.PopulateRootDirectoryAsync, controller.HandleRootDirectoryActionAsync)
           .AddSensor(PlayerWidgetConstants.CoverArtSensorName, null, controller.GetCoverArtAsync)
           .AddSensor(PlayerWidgetConstants.TitleSensorName, null, controller.GetTitleAsync)
           .AddSensor(PlayerWidgetConstants.DescriptionSensorName, null, controller.GetDescriptionAsync)
           .AddSwitch(PlayerWidgetConstants.PlayingSwitchName, null, controller.GetIsPlayingAsync, controller.SetIsPlayingAsync)
           .AddSwitch(PlayerWidgetConstants.MuteSwitchName, null, controller.GetIsMutedAsync, controller.SetIsMutedAsync)
           .AddSwitch(PlayerWidgetConstants.ShuffleSwitchName, null, controller.GetShuffleAsync, controller.SetShuffleAsync)
           .AddSwitch(PlayerWidgetConstants.RepeatSwitchName, null, controller.GetRepeatAsync, controller.SetRepeatAsync);
    }

    private DeviceBuilder AddPowerStateSensor(DeviceValueGetter<bool> getter)
    {
        SensorParameters parameters = new(
            SensorType.Power,
            Constants.PowerSensorName,
            DeviceBuilderConstants.PowerSensorLabel,
            ValueFeature.Create(getter)
        );
        if (!this._sensors.TryAdd(Constants.PowerSensorName, parameters))
        {
            throw new InvalidOperationException("PowerState sensor already added.");
        }
        return this;
    }

    private DeviceBuilder AddSensor(string name, string? label, DeviceValueGetter<bool> getter) => this.AddSensor(name, SensorType.Binary, label, getter);

    private DeviceBuilder AddSensor(string name, string? label, DeviceValueGetter<string> getter) => this.AddSensor(name, SensorType.String, label, getter);

    private DeviceBuilder AddSensor(string name, string? label, DeviceValueGetter<object> getter) => this.AddSensor(name, SensorType.Custom, label, getter);

    private DeviceBuilder AddSensor<T>(string name, SensorType type, string? label, DeviceValueGetter<T> getter) where T : notnull => this.AddSensorParameters(
        name,
        new(type, Validator.ValidateText(name), Validator.ValidateText(label, allowNull: true), ValueFeature.Create(getter))
    );

    private DeviceBuilder AddSensor(string name, string? label, DeviceValueGetter<double> getter, double rangeLow, double rangeHigh, string unit) => this.AddSensorParameters(
        name,
        new RangeSensorParameters(
            Validator.ValidateText(name),
            Validator.ValidateText(label, allowNull: true),
            ValueFeature.Create(getter),
            Validator.ValidateRange(rangeLow, rangeHigh),
            Validator.ValidateText(unit)
        )
    );

    private DeviceBuilder AddSensorParameters(string name, SensorParameters parameters)
    {
        if (name == Constants.PowerSensorName)
        {
            throw new ArgumentException($"Name can not be {Constants.PowerSensorName}.", nameof(name));
        }
        if (!this._sensors.TryAdd(name, parameters))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        return this;
    }

    private DeviceBuilder AddSlider(string name, string? label, DeviceValueGetter<double> getter, DeviceValueSetter<double> setter, double rangeLow, double rangeHigh, string unit)
    {
        SliderParameters parameters = new(
            Validator.ValidateText(name),
            Validator.ValidateText(label, allowNull: true),
            ValueFeature.Create(getter, setter),
            Validator.ValidateRange(rangeLow, rangeHigh),
            Validator.ValidateText(unit)
        );
        if (!this._sliders.TryAdd(name, parameters))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        return this;
    }

    private DeviceBuilder AddSmartApplicationButton(SmartApplicationButtons buttons) => SmartAppButton.GetNames(buttons).Aggregate(
        this,
        static (builder, name) => builder.AddButton(name)
    );

    private DeviceBuilder AddSwitch(string name, string? label, DeviceValueGetter<bool> getter, DeviceValueSetter<bool> setter)
    {
        SwitchParameters parameters = new(
            Validator.ValidateText(name),
            Validator.ValidateText(label, allowNull: true),
            ValueFeature.Create(getter, setter)
        );
        if (!this._switches.TryAdd(name, parameters))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        return this;
    }

    private DeviceBuilder AddTextLabel(string name, string? label, DeviceValueGetter<string> getter, bool? isLabelVisible)
    {
        TextLabelParameters parameters = new(
            Validator.ValidateText(name),
            Validator.ValidateText(label, allowNull: true),
            ValueFeature.Create(getter),
            isLabelVisible
        );
        if (!this._textLabels.TryAdd(name, parameters))
        {
            throw new ArgumentException($"\"{name}\" already defined.", nameof(name));
        }
        return this;
    }

    private DeviceAdapter BuildAdapter()
    {
        if (this.ButtonHandler == null && this._buttons.Any())
        {
            throw new InvalidOperationException($"There are buttons defined but no handler was specified (by calling {nameof(IDeviceBuilder.AddButtonHandler)}.");
        }
        if (this.Type.RequiresInput() && !this._hasInput)
        {
            throw new InvalidOperationException($"No input buttons defined - note that input button names must begin with \"{DeviceBuilderConstants.InputPrefix}\".");
        }
        if (this.Characteristics.Contains(DeviceCharacteristic.BridgeDevice) && this.Setup.RegistrationType is null)
        {
            throw new InvalidOperationException($"A device with characteristic {DeviceCharacteristic.BridgeDevice} must support registration (by calling {nameof(IDeviceBuilder.EnableRegistration)}).");
        }
        List<DeviceCapability> deviceCapabilities = this.Characteristics.Select(static characteristic => (DeviceCapability)characteristic).ToList();
        string pathPrefix = $"/device/{this.AdapterName}/";
        HashSet<string> paths = new();
        List<Component> components = new();
        Dictionary<string, IFeature> features = new();
        foreach ((string name, string? label) in this._buttons.Values)
        {
            AddComponentAndRouteHandler(BuildButton(pathPrefix, name, label), new ButtonFeature(this.ButtonHandler!, name));
        }
        foreach ((string name, string? label, ValueFeature valueFeature, IReadOnlyCollection<double> range, string unit) in this._sliders.Values)
        {
            AddComponentAndRouteHandler(BuildSensor(pathPrefix, name, label, new RangeSensorDetails(range, unit)), valueFeature);
            AddComponentAndRouteHandler(BuildSlider(pathPrefix, name, label, range, unit), valueFeature);
        }
        foreach ((string name, string? label, ValueFeature valueFeature) in this._switches.Values)
        {
            AddComponentAndRouteHandler(BuildSensor(pathPrefix, name, label, new(SensorType.Binary)), valueFeature);
            AddComponentAndRouteHandler(BuildSwitch(pathPrefix, name, label), valueFeature);
        }
        foreach ((string name, string? label, ValueFeature valueFeature, bool? isLabelVisible) in this._textLabels.Values)
        {
            AddComponentAndRouteHandler(BuildSensor(pathPrefix, name, label, new(SensorType.String)), valueFeature);
            AddComponentAndRouteHandler(BuildTextLabel(pathPrefix, name, label, isLabelVisible), valueFeature);
        }
        foreach ((string name, string? label, ValueFeature valueFeature, ImageSize size, string? uri) in this._imageUrls.Values)
        {
            AddComponentAndRouteHandler(BuildSensor(pathPrefix, name, label, new(SensorType.String)), valueFeature);
            AddComponentAndRouteHandler(BuildImageUrl(pathPrefix, name, label, size, uri), valueFeature);
        }
        foreach (DirectoryParameters parameters in this._directories.Values)
        {
            AddComponentAndRouteHandler(BuildDirectory(pathPrefix, parameters), parameters.Feature);
        }
        foreach (SensorParameters parameters in this._sensors.Values)
        {
            AddComponentAndRouteHandler(
                parameters switch
                {
                    { Type: SensorType.Power } => BuildPowerSensor(pathPrefix),
                    RangeSensorParameters rsd => BuildSensor(pathPrefix, rsd.Name, rsd.Label, new RangeSensorDetails(rsd.Range, rsd.Unit)),
                    _ => BuildSensor(pathPrefix, parameters.Name, parameters.Label, new(parameters.Type))
                },
                parameters.ValueFeature
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
            this._routeHandler,
            this.Setup,
            this.SpecificName,
            this.Timing ?? new(),
            this.AdditionalSearchTokens,
            this.Type,
            this._uriPrefixCallback
        );

        void AddComponentAndRouteHandler(Component component, IFeature feature)
        {
            AddRouteHandler(component, feature);
            components.Add(component);
        }

        void AddRouteHandler(Component component, IFeature feature) => features[Uri.UnescapeDataString(component.Name)] = paths.Add(component.Path)
            ? feature
            : throw new InvalidOperationException($"Path {component.Path} is not unique.");

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

        static DirectoryComponent BuildDirectory(string pathPrefix, DirectoryParameters parameters)
        {
            (string name, string? label, DirectoryRole? role, _) = parameters;
            string directoryName = Uri.EscapeDataString(name);
            string path = pathPrefix + directoryName;
            return new(
                directoryName,
                label is { } text ? Uri.EscapeDataString(text) : directoryName,
                path,
                role
            );
        }

        static ImageUrlComponent BuildImageUrl(string pathPrefix, string name, string? label, ImageSize size, string? uri)
        {
            string imageName = Uri.EscapeDataString(name);
            string path = pathPrefix + imageName;
            return new(
                imageName,
                label is { } text ? Uri.EscapeDataString(text) : imageName,
                path,
                uri,
                size,
                GetSensorName(imageName)
            );
        }

        static SensorComponent BuildPowerSensor(string pathPrefix)
        {
            SensorComponent component = BuildSensor(pathPrefix, "-", DeviceBuilderConstants.PowerSensorLabel, new(SensorType.Power));
            /*
                Power state sensors are added by AddPowerStateSensor with the name "powerstate".
                For backward compatibility, we need to avoid changing it to "POWERSTATE_SENSOR".
            */
            return component with { Name = Constants.PowerSensorName, Path = pathPrefix + Constants.PowerSensorName };
        }

        static TextLabelComponent BuildTextLabel(string pathPrefix, string name, string? label, bool? isLabelVisible)
        {
            string textLabelName = Uri.EscapeDataString(name);
            string path = pathPrefix + textLabelName;
            return new(textLabelName, label is { } text ? Uri.EscapeDataString(text) : textLabelName, path, isLabelVisible, GetSensorName(textLabelName));
        }

        static SensorComponent BuildSensor(string pathPrefix, string name, string? label, SensorDetails sensor)
        {
            string sensorName = GetSensorName(Uri.EscapeDataString(name));
            string path = pathPrefix + sensorName;
            return new(sensorName, Uri.EscapeDataString(label ?? name), path, sensor);
        }

        static SliderComponent BuildSlider(string pathPrefix, string name, string? label, IReadOnlyCollection<double> range, string unit)
        {
            string sliderName = Uri.EscapeDataString(name);
            string path = pathPrefix + sliderName;
            return new(sliderName, label is { } text ? Uri.EscapeDataString(text) : sliderName, path, new(range, Uri.EscapeDataString(unit), GetSensorName(sliderName)));
        }

        static SwitchComponent BuildSwitch(string pathPrefix, string name, string? label)
        {
            string switchName = Uri.EscapeDataString(name);
            string path = pathPrefix + switchName;
            return new(switchName, Uri.EscapeDataString(label ?? string.Empty), path, GetSensorName(switchName));
        }

        static string GetSensorName(string name) => name.EndsWith(SensorDetails.ComponentSuffix) ? name : string.Concat(name.ToUpperInvariant(), SensorDetails.ComponentSuffix);

        static bool RequiresDiscovery(DeviceCapability capability) => capability is DeviceCapability.BridgeDevice or DeviceCapability.AddAnotherDevice or DeviceCapability.RegisterUserAccount;
    }

    private DeviceBuilder DefineTiming(int? powerOnDelay, int? shutdownDelay, int? sourceSwitchDelay)
    {
        if (!this.Type.SupportsTiming())
        {
            throw new NotSupportedException($"Device type {this.Type} does not support timing.");
        }
        if (this.Timing.HasValue)
        {
            throw new InvalidOperationException("Timing is already defined.");
        }
        this.Timing = new(Validator.ValidateDelay(powerOnDelay), Validator.ValidateDelay(shutdownDelay), Validator.ValidateDelay(sourceSwitchDelay));
        return this;
    }

    private DeviceBuilder EnableDeviceRoute(UriPrefixCallback uriPrefixCallback, DeviceRouteHandler routeHandler)
    {
        if (this._uriPrefixCallback is not null)
        {
            throw new InvalidOperationException("Device route already defined.");
        }
        (this._uriPrefixCallback, this._routeHandler) = (uriPrefixCallback, routeHandler);
        return this;
    }

    private DeviceBuilder EnableDiscovery(string headerText, string summary, DiscoveryProcess process, bool enableDynamicDeviceBuilder)
    {
        if (this.Setup.Discovery.HasValue)
        {
            throw new InvalidOperationException("Discovery is already defined.");
        }
        this.Setup.Discovery = true;
        this.Setup.DiscoveryHeaderText = Validator.ValidateText(headerText, maxLength: 255);
        this.Setup.DiscoverySummary = Validator.ValidateText(summary, maxLength: 255);
        this.Setup.EnableDynamicDeviceBuilder = enableDynamicDeviceBuilder;
        this.DiscoveryFeature = new(process, enableDynamicDeviceBuilder);
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
        where TPayload : struct
    {
        if (this.Setup.RegistrationType.HasValue)
        {
            throw new InvalidOperationException("Registration is already defined.");
        }
        if (!this.Setup.Discovery.HasValue)
        {
            throw new InvalidOperationException($"Registration is only supported on devices with discovery. (Call {nameof(IDeviceBuilder.EnableDiscovery)} first).");
        }
        this.Setup.RegistrationHeaderText = Validator.ValidateText(headerText, maxLength: 255);
        this.Setup.RegistrationSummary = Validator.ValidateText(description, maxLength: 255);
        this.RegistrationFeature = RegistrationFeature.Create(queryIsRegistered, processor);
        this.Setup.RegistrationType = type;
        return this;
    }

    private DeviceBuilder RegisterDeviceSubscriptionCallbacks(DeviceSubscriptionHandler onDeviceAdded, DeviceSubscriptionHandler onDeviceRemoved, DeviceSubscriptionListHandler initializeDeviceList)
    {
        if (this.SubscriptionFeature != null)
        {
            throw new InvalidOperationException($"{nameof(this.RegisterDeviceSubscriptionCallbacks)} was already called.");
        }
        this.SubscriptionFeature = new(onDeviceAdded, onDeviceRemoved, initializeDeviceList);
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
        this.Manufacturer = Validator.ValidateText(manufacturer);
        return this;
    }

    private DeviceBuilder SetSpecificName(string? specificName)
    {
        this.SpecificName = Validator.ValidateText(specificName, allowNull: true);
        return this;
    }

    private sealed record ButtonParameters(string Name, string? Label) : ParametersBase(Name, Label);

    private record struct Credentials([property: JsonPropertyName("username")] string UserName, string Password);

    private static class DeviceBuilderConstants
    {
        public const string InputPrefix = "INPUT";
        public const string PowerSensorLabel = "Powerstate";
    }

    private sealed record DirectoryParameters(string Name, string? Label, DirectoryRole? Role, DirectoryFeature Feature) : ParametersBase(Name, Label);

    private sealed record ImageUrlParameters(string Name, string? Label, ValueFeature Feature, ImageSize Size, String? Uri) : ParametersBase(Name, Label);

    private abstract record ParametersBase(string Name, string? Label);

    private sealed record RangeSensorParameters(string Name, string? Label, ValueFeature Feature, IReadOnlyCollection<double> Range, string Unit) : SensorParameters(SensorType.Range, Name, Label, Feature);

    private record struct SecurityCodeContainer(string SecurityCode);

    private record SensorParameters(SensorType Type, string Name, string? Label, ValueFeature ValueFeature) : ParametersBase(Name, Label);

    private sealed record SliderParameters(string Name, string? Label, ValueFeature Feature, IReadOnlyCollection<double> Range, string Unit) : ParametersBase(Name, Label);

    private sealed record SwitchParameters(string Name, string? Label, ValueFeature Feature) : ParametersBase(Name, Label);

    private sealed record TextLabelParameters(string Name, string? Label, ValueFeature Feature, bool? IsLabelVisible) : ParametersBase(Name, Label);
}