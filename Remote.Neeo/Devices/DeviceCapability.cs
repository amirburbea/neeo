﻿using System.ComponentModel;
using System.Text.Json.Serialization;
using Remote.Neeo.Devices.Discovery;

namespace Remote.Neeo.Devices
{
    /// <summary>
    /// Specifies special capabilities supported by the device.
    /// </summary>
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
        /// Drivers with this capability do not need to specify 'POWER ON'/'POWER OFF' buttons and the device is not identified as "stupid".
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
        /// The device has a custom favorites handler. (See <see cref="IDeviceBuilder.SetFavoritesHandler"/>).
        /// <para />
        /// This should only be used internally by the API and not added directly via <see cref="IDeviceBuilder.AddCapability"/>.
        /// </summary>
        [Text("customFavoriteHandler"), Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        CustomFavoriteHandler,

        /// <summary>
        /// If <see cref="DiscoveryOptions.EnableDynamicDeviceBuilder"/> is enabled, dynamically defined devices should set this capability.
        /// </summary>
        [Text("dynamicDevice")]
        DynamicDevice,

        /// <summary>
        /// The device uses registration (<see cref="IDeviceBuilder.EnableRegistration"/>).
        /// <para />
        /// This should only be used internally by the API and not added directly via <see cref="IDeviceBuilder.AddCapability"/>.
        /// </summary>
        [Text("register-user-account"), Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        RegisterUserAccount,
    }
}