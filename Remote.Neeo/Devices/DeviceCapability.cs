using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices
{
    [JsonConverter(typeof(TextAttribute.EnumJsonConverter<DeviceCapability>))]
    public enum DeviceCapability
    {
        /// <summary>
        /// This capability is used after you add a new device that uses discovery. It gives the option to select &quot;Add another {name}.&quot;.
        /// </summary>
        [Text("addAnotherDevice")]
        AddAnotherDevice = StaticDeviceCapability.AddAnotherDevice,

        /// <summary>
        /// This capability is used when the device does not need to be powered on to be useable.
        /// <para />
        /// You don't need to specify 'POWER ON' and 'POWER OFF' buttons and the device is not identified as "stupid".
        /// </summary>
        [Text("alwaysOn")]
        AlwaysOn = StaticDeviceCapability.AlwaysOn,

        /// <summary>
        /// This capability is used after you add a hub/gateway device. It gives the option to select &quot;Add more from this bridge&quot;.
        /// <para />
        /// Example: Philips Hue - the discovered device (gateway) supports multiple devices (lamps).
        /// </summary>
        [Text("bridgeDevice")]
        BridgeDevice = StaticDeviceCapability.BridgeDevice,

        /// <summary>
        /// Dynamically defined devices via [DynamicDeviceBuilderEnabled] should define this option.
        /// </summary>
        [Text("dynamicDevice")]
        DynamicDevice = StaticDeviceCapability.DynamicDevice,

        [Text("customFavoriteHandler")]
        CustomFavoriteHandler,

        [Text("register-user-account")]
        RegisterUserAccount,
    }

    public enum StaticDeviceCapability
    {
        AddAnotherDevice,        
        AlwaysOn,
        BridgeDevice ,
        DynamicDevice,
    }
}
