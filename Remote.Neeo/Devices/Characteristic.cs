using System.ComponentModel;
using System.Text.Json.Serialization;
using Remote.Neeo.Devices.Discovery;

namespace Remote.Neeo.Devices
{
    /// <summary>
    /// Specifies special/unique characteristics supported by the device.
    /// </summary>
    [JsonConverter(typeof(TextAttribute.EnumJsonConverter<Characteristic>))]
    public enum Characteristic
    {
        /// <summary>
        /// This characteristic is optionally specified for devices that uses discovery.
        /// It gives the option to select &quot;Add another {name}.&quot;.
        /// </summary>
        [Text("addAnotherDevice")]
        AddAnotherDevice,

        /// <summary>
        /// This characteristic is used when the device does not need to be powered on to be useable.
        /// </summary>
        /// <remarks>
        /// Drivers with this characteristic do not need to specify &quot;POWER ON&quot;/&quot;POWER OFF&quot;
        /// buttons and will not be identified as &quot;stupid&quot;.
        /// </remarks>
        [Text("alwaysOn")]
        AlwaysOn,

        /// <summary>
        /// This characteristic is used after you add a hub/gateway device. 
        /// It gives the option to select &quot;Add more from this bridge&quot;.
        /// <para />
        /// Example: Philips Hue - the discovered device (gateway) supports multiple devices (lamps).
        /// </summary>
        [Text("bridgeDevice")]
        BridgeDevice,

        /// <summary>
        /// The device has a custom favorites handler. (See <see cref="IDeviceBuilder.RegisterFavoritesHandler"/>).
        /// </summary>
        /// <remarks>
        /// This should only be used internally by the API and not added directly via 
        /// <see cref="IDeviceBuilder.AddCharacteristic"/>.
        /// </remarks>
        [Text("customFavoriteHandler"), Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        CustomFavoriteHandler,

        /// <summary>
        /// If <see cref="DiscoveryOptions.EnableDynamicDeviceBuilder"/> is enabled, dynamically defined devices should
        /// specify this characteristic.
        /// </summary>
        [Text("dynamicDevice")]
        DynamicDevice,

        /// <summary>
        /// The device uses registration (<see cref="IDeviceBuilder.EnableRegistration"/>).
        /// </summary>
        /// <remarks>
        /// This should only be used internally by the API and not added directly via 
        /// <see cref="IDeviceBuilder.AddCharacteristic"/>.
        /// </remarks>
        [Text("register-user-account"), Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        RegisterUserAccount,
    }
}
