namespace Neeo.Sdk.Devices;

/// <summary>
/// Specifies special/unique characteristics supported by the device.
/// </summary>
public enum DeviceCharacteristic
{
    /// <summary>
    /// This characteristic is used when the device does not need to be powered on to be usable.
    /// </summary>
    /// <remarks>
    /// Drivers with this characteristic do not need to specify &quot;POWER ON&quot;/&quot;POWER OFF&quot;/&quot;POWER TOGGLE&quot;
    /// buttons and will not be identified as &quot;stupid&quot;.
    /// </remarks>
    AlwaysOn = 0,

    /// <summary>
    /// This characteristic is optionally specified for devices that uses discovery.
    /// It gives the option to select &quot;Add another {name}.&quot;.
    /// </summary>
    /// <remarks>Devices specifying this characteristic must enable discovery via <see cref="IDeviceBuilder.EnableDiscovery"/>.</remarks>
    AddAnotherDevice = 2,

    /// <summary>
    /// This characteristic is used after you add a hub/gateway device.
    /// It gives the option to select &quot;Add more from this bridge&quot;.
    /// <para />
    /// Example: Philips Hue - the discovered device (gateway) supports multiple devices (lamps).
    /// </summary>
    /// <remarks>Devices specifying this characteristic must enable discovery via <see cref="IDeviceBuilder.EnableDiscovery"/>.</remarks>
    BridgeDevice = 3,

    /// <summary>
    /// If <see cref="DeviceSetup.EnableDynamicDeviceBuilder"/> is enabled, dynamically defined devices should specify this characteristic.
    /// </summary>
    DynamicDevice = 4
}
