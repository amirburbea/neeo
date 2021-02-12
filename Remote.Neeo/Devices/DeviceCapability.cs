using System.ComponentModel;
using System.Text.Json.Serialization;
using Remote.Neeo.Devices.Discovery;

namespace Remote.Neeo.Devices
{
    [JsonConverter(typeof(TextAttribute.EnumJsonConverter<DeviceCapability>))]
    public enum DeviceCapability
    {
        /// <summary>
        /// This capability is used after you add a new device that uses discovery. It gives the option to select &quot;Add another {name}.&quot;.
        /// </summary>
        [Text("addAnotherDevice")]
        AddAnotherDevice,

        /// <summary>
        /// This capability is used when the device does not need to be powered on to be useable.
        /// <para />
        /// You don't need to specify 'POWER ON' and 'POWER OFF' buttons and the device is not identified as "stupid".
        /// </summary>
        [Text("alwaysOn")]
        AlwaysOn,

        /// <summary>
        /// This capability is used after you add a hub/gateway device. It gives the option to select &quot;Add more from this bridge&quot;.
        /// <para />
        /// Example: Philips Hue - the discovered device (gateway) supports multiple devices (lamps).
        /// </summary>
        [Text("bridgeDevice")]
        BridgeDevice,

        /// <summary>
        /// If <see cref="DiscoveryOptions.EnableDynamicDeviceBuilder"/> is enabled, dynamically defined devices should set this capability.
        /// </summary>
        [Text("dynamicDevice")]
        DynamicDevice,

        [Text("customFavoriteHandler"), Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        CustomFavoriteHandler,

        [Text("register-user-account"), Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        RegisterUserAccount,
    }
}
