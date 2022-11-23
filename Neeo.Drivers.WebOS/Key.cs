using System;
using System.Collections.Generic;

namespace Neeo.Drivers.WebOS;

public enum Key
{
    ArrowDown,
    ArrowLeft,
    ArrowRight,
    ArrowUp,
    AspectRatio,
    AudioMode,

    [Key("returnback")]
    Back,

    [Key("bluebutton")]
    Blue,

    CaptionSubtitle,
    ChannelDown,
    ChannelList,
    ChannelUp,
    DeviceInput,

    [Key("screenbright")]
    EnerySaving,

    FastForward,

    [Key("greenbutton")]
    Green,

    [Key("myapp")]
    Home,

    [Key("programinfo")]
    Info,

    LiveTV,

    [Key("settingmenu")]
    Menu,

    Number0,
    Number1,
    Number2,
    Number3,
    Number4,
    Number5,
    Number6,
    Number7,
    Number8,
    Number9,
    OK,
    Play,
    PreviousChannel,
    ProgramGuide,
    Record,

    [Key("redbutton")]
    Red,

    Rewind,

    [Key("sleepreserve")]
    SleepTimer,

    UserGuide,
    VideoMode,
    VolumeDown,
    VolumeMute,
    VolumeUp,

    [Key("yellowbutton")]
    Yellow,
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
internal sealed class KeyAttribute : Attribute, INameAttribute
{
    public KeyAttribute(string key) => this.Key = key;

    public string Key { get; }

    string INameAttribute.Name => this.Key;
}

public static class KeyName
{
    private static readonly IReadOnlyDictionary<Key, string> _names = NameDictionary.Generate<Key, KeyAttribute>();

    public static string Of(Key key) => KeyName._names[key];
}