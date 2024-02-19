using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Device Types.
/// </summary>
[JsonConverter(typeof(TextJsonConverter<DeviceType>))]
public enum DeviceType
{
    /// <summary>
    /// Accessory device.
    /// </summary>
    [Text("ACCESSOIRE")]
    Accessory = 0,

    /// <summary>
    /// Audio device.
    /// </summary>
    [Text("AUDIO")]
    Audio,

    /// <summary>
    /// A/V Receiver or similar device.
    /// </summary>
    [Text("AVRECEIVER")]
    AVReceiver,

    /// <summary>
    /// DVD, Blu-Ray, or similar device.
    /// </summary>
    [Text("DVD")]
    DvdDisc,

    /// <summary>
    /// Game Console or similar device.
    /// </summary>
    [Text("GAMECONSOLE")]
    GameConsole,

    /// <summary>
    /// Light or similar device.
    /// </summary>
    [Text("LIGHT")]
    Light,

    /// <summary>
    /// Media Player or similar device.
    /// </summary>
    [Text("MEDIAPLAYER")]
    MediaPlayer,

    /// <summary>
    /// Music Player or similar device.
    /// </summary>
    [Text("MUSICPLAYER")]
    MusicPlayer,

    /// <summary>
    /// Projector or similar device.
    /// </summary>
    [Text("PROJECTOR")]
    Projector,

    /// <summary>
    /// TV or similar device.
    /// </summary>
    [Text("TV")]
    TV,

    /// <summary>
    /// Video-On-Demand box such as FireTV or similar device.
    /// </summary>
    [Text("VOD")]
    VideoOnDemand,

    /// <summary>
    /// HDMI Switcher or similar device.
    /// </summary>
    [Text("HDMISWITCH")]
    HdmiSwitch,

    /// <summary>
    /// Cable/Satellite Box or similar device.
    /// </summary>
    [Text("DVB")]
    SetTopBox,

    /// <summary>
    /// Soundbar or similar device.
    /// </summary>
    [Text("SOUNDBAR")]
    SoundBar,

    /// <summary>
    /// Radio Tuner or similar device.
    /// </summary>
    [Text("TUNER")]
    Tuner,
}

/// <summary>
/// Static extension methods for the <see cref="DeviceType"/> enumerated type.
/// </summary>
public static class DeviceTypes
{
    /// <summary>
    /// Gets a value indicating if the specified device <paramref name="type"/> requires specifying at least one input command.
    /// </summary>
    /// <param name="type">The type of the device.</param>
    /// <returns>Boolean value.</returns>
    public static bool RequiresInput(this DeviceType type) => type
        is DeviceType.AVReceiver
        or DeviceType.HdmiSwitch
        or DeviceType.Projector
        or DeviceType.SoundBar
        or DeviceType.TV;

    /// <summary>
    /// Gets a value indicating if the specified device <paramref name="type"/> supports favorites
    /// (limited to <see cref="DeviceType.SetTopBox"/>, <see cref="DeviceType.Tuner"/> and <see cref="DeviceType.TV"/>).
    /// </summary>
    /// <param name="type">The type of the device.</param>
    /// <returns>Boolean value.</returns>
    public static bool SupportsFavorites(this DeviceType type) => type
        is DeviceType.SetTopBox
        or DeviceType.Tuner
        or DeviceType.TV;

    /// <summary>
    /// Gets a value indicating if the specified device <paramref name="type"/> supports the player widgets
    /// (limited to <see cref="DeviceType.MediaPlayer"/>, <see cref="DeviceType.MusicPlayer"/> and <see cref="DeviceType.VideoOnDemand"/>).
    /// </summary>
    /// <param name="type">The type of the device.</param>
    /// <returns>Boolean value.</returns>
    public static bool SupportsPlayerWidget(this DeviceType type) => type
        is DeviceType.MediaPlayer
        or DeviceType.MusicPlayer
        or DeviceType.VideoOnDemand;

    /// <summary>
    /// Gets a value indicating if the specified device <paramref name="type"/> supports timing -
    /// the delays the NEEO Brain should use when interacting with the device.
    /// </summary>
    /// <param name="type">The type of the device.</param>
    /// <returns>Boolean value.</returns>
    public static bool SupportsTiming(this DeviceType type) => type
        is not DeviceType.Accessory
        and not DeviceType.Light;
}
