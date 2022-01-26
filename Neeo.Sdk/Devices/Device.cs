namespace Neeo.Sdk.Devices;

using static DeviceType;

/// <summary>
/// A set of <see langword="static"/> methods and utilities related to device creation.
/// </summary>
public static class Device
{
    /// <summary>
    /// Gets a fluent <see cref="IDeviceBuilder"/> for defining a NEEO device driver.
    /// </summary>
    /// <param name="name">The name of the device.</param>
    /// <param name="type">The type of device.</param>
    /// <param name="prefix">
    /// An optional prefix to attach to the internal name (defaults to the computer host name).
    /// <para />
    /// If the driver will be developed/debugged on one computer, but generally run from another (such as a Raspberry PI),
    /// it may be beneficial to specify a constant prefix.
    /// </param>
    /// <returns><see cref="IDeviceBuilder"/> for defining the NEEO device driver.</returns>
    public static IDeviceBuilder CreateDevice(string name, DeviceType type, string? prefix = default) => new DeviceBuilder(name, type, prefix);

    /// <summary>
    /// Gets a value indicating if the specified device <paramref name="type"/> requires specifying at least one input command.
    /// </summary>
    /// <param name="type">The type of the device.</param>
    /// <returns>Boolean value.</returns>
    public static bool RequiresInput(this DeviceType type) => type is AVReceiver or HdmiSwitch or Projector or SoundBar or TV;

    /// <summary>
    /// Gets a value indicating if the specified device <paramref name="type"/> supports favorites
    /// (limited to <see cref="SetTopBox"/>, <see cref="Tuner"/> and <see cref="TV"/>).
    /// </summary>
    /// <param name="type">The type of the device.</param>
    /// <returns>Boolean value.</returns>
    public static bool SupportsFavorites(this DeviceType type) => type is SetTopBox or Tuner or TV;

    /// <summary>
    /// Gets a value indicating if the specified device <paramref name="type"/> supports the player widgets
    /// (limited to <see cref="MediaPlayer"/>, <see cref="MusicPlayer"/> and <see cref="VideoOnDemand"/>).
    /// </summary>
    /// <param name="type">The type of the device.</param>
    /// <returns>Boolean value.</returns>
    public static bool SupportsPlayer(this DeviceType type) => type is MediaPlayer or MusicPlayer or VideoOnDemand;

    /// <summary>
    /// Gets a value indicating if the specified device <paramref name="type"/> supports timing -
    /// the delays the NEEO Brain should use when interacting with the device.
    /// </summary>
    /// <param name="type">The type of the device.</param>
    /// <returns>Boolean value.</returns>
    public static bool SupportsTiming(this DeviceType type) => type is not Accessory and not Light;
}
