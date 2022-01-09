using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices;

/// <summary>
/// Device Types.
/// </summary>
[JsonConverter(typeof(TextAttribute.EnumJsonConverter<DeviceType>))]
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
    DVDisc,

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
