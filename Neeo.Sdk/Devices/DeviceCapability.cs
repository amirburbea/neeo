using System.Text.Json.Serialization;
using Neeo.Sdk.Devices.Discovery;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

/// <summary>
/// The unique/special capabilities supported by the device.
/// </summary>
[JsonConverter(typeof(TextJsonConverter<DeviceCapability>))]
public enum DeviceCapability
{
    /// <summary>
    /// This characteristic is optionally specified for devices that uses discovery.
    /// It gives the option to select &quot;Add another {name}.&quot;.
    /// </summary>
    [Text("addAnotherDevice")]
    AddAnotherDevice = DeviceCharacteristic.AddAnotherDevice,

    /// <summary>
    /// This characteristic is used when the device does not need to be powered on to be usable.
    /// </summary>
    /// <remarks>
    /// Drivers with this characteristic do not need to specify &quot;POWER ON&quot;/&quot;POWER OFF&quot;/&quot;POWER TOGGLE&quot;
    /// buttons and will not be identified as &quot;stupid&quot;.
    /// </remarks>
    [Text("alwaysOn")]
    AlwaysOn = DeviceCharacteristic.AlwaysOn,

    /// <summary>
    /// This characteristic is used after you add a hub/gateway device.
    /// It gives the option to select &quot;Add more from this bridge&quot;.
    /// <para />
    /// Example: Philips Hue - the discovered device (gateway) supports multiple devices (lamps).
    /// </summary>
    [Text("bridgeDevice")]
    BridgeDevice = DeviceCharacteristic.BridgeDevice,

    /// <summary>
    /// If <see cref="IDeviceSetup.EnableDynamicDeviceBuilder"/> is enabled, dynamically defined devices should
    /// specify this characteristic.
    /// </summary>
    [Text("dynamicDevice")]
    DynamicDevice = DeviceCharacteristic.DynamicDevice,

    /// <summary>
    /// The device has a custom favorites handler. (See <see cref="IDeviceBuilder.RegisterFavoritesHandler"/>).
    /// </summary>
    [Text("customFavoriteHandler")]
    CustomFavoriteHandler,

    /// <summary>
    /// The device uses registration.
    /// (See <see cref="IDeviceBuilder.EnableRegistration(string, string, QueryIsRegistered, CredentialsRegistrationProcessor)"/>
    /// or <see cref="IDeviceBuilder.EnableRegistration(string, string, QueryIsRegistered, SecurityCodeRegistrationProcessor)"/>).
    /// </summary>
    [Text("register-user-account")]
    RegisterUserAccount,
}