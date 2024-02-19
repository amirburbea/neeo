using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Neeo.Drivers.Kodi;

public enum InputCommand
{
    [InputCommand("Input.Back")]
    Back,

    [InputCommand("Input.ExecuteAction", "blue")]
    Blue,

    [InputCommand("Input.ExecuteAction", "channeldown")]
    ChannelDown,

    [InputCommand("Input.ExecuteAction", "nextchannelgroup")]
    ChannelSearch,

    [InputCommand("Input.ExecuteAction", "channelup")]
    ChannelUp,

    [InputCommand("Input.ExecuteAction", "codecinfo")]
    CodecInfo,

    [InputCommand("Input.Down")]
    Down,

    [InputCommand("Input.ExecuteAction", "green")]
    Green,

    [InputCommand("Input.ExecuteAction", "info")]
    Info,

    [InputCommand("Input.ExecuteAction", "audionextlanguage")]
    Language,

    [InputCommand("Input.Left")]
    Left,

    [InputCommand("Input.ContextMenu")]
    Menu,

    [InputCommand("Input.ExecuteAction", "mute")]
    MuteToggle,

    [InputCommand("Input.ExecuteAction", "number0")]
    Number0,

    [InputCommand("Input.ExecuteAction", "number1")]
    Number1,

    [InputCommand("Input.ExecuteAction", "number2")]
    Number2,

    [InputCommand("Input.ExecuteAction", "number3")]
    Number3,

    [InputCommand("Input.ExecuteAction", "number4")]
    Number4,

    [InputCommand("Input.ExecuteAction", "number5")]
    Number5,

    [InputCommand("Input.ExecuteAction", "number6")]
    Number6,

    [InputCommand("Input.ExecuteAction", "number7")]
    Number7,

    [InputCommand("Input.ExecuteAction", "number8")]
    Number8,

    [InputCommand("Input.ExecuteAction", "number9")]
    Number9,

    [InputCommand("Input.xxxx")]
    NumberSeperator,

    [InputCommand("Input.ExecuteAction", "osd")]
    OnScreenDisplay,

    [InputCommand("Input.ExecuteAction", "pause")]
    Pause,

    [InputCommand("Input.ExecuteAction", "play")]
    Play,

    [InputCommand("Input.PlayPause")]
    PlayPauseToggle,

    [InputCommand("Input.ExecuteAction", "red")]
    Red,

    [InputCommand("Input.Right")]
    Right,

    [InputCommand("Input.Select")]
    Select,

    [InputCommand("Input.ExecuteAction", "skipnext")]
    SkipNext,

    [InputCommand("Input.ExecuteAction", "skipprevious")]
    SkipPrevious,

    [InputCommand("Input.ExecuteAction", "stop")]
    Stop,

    [InputCommand("Input.ExecuteAction", "nextsubtitle")]
    Subtitle,

    [InputCommand("Input.ExecuteAction", "togglefullscreen")]
    ToggleFullScreen,

    [InputCommand("Input.Up")]
    Up,

    [InputCommand("Input.ExecuteAction", "volumedown")]
    VolumeDown,

    [InputCommand("Input.ExecuteAction", "volumeup")]
    VolumeUp,

    [InputCommand("Input.ExecuteAction", "yellow")]
    Yellow,
}

[AttributeUsage(AttributeTargets.Field)]
internal sealed class InputCommandAttribute : Attribute
{
    private static readonly Dictionary<InputCommand, InputCommandAttribute> _attributes = new(
        from field in typeof(InputCommand).GetFields(BindingFlags.Static | BindingFlags.Public)
        let attribute = field.GetCustomAttribute<InputCommandAttribute>()
        where attribute != null
        select KeyValuePair.Create((InputCommand)field.GetValue(null)!, attribute)
    );

    public InputCommandAttribute(string method, string? action = default)
    {
        (this.Method, this.Action) = (method, action);
    }

    public string? Action { get; }

    public string Method { get; }

    public static InputCommandAttribute? GetAttribute(InputCommand command) => InputCommandAttribute._attributes.GetValueOrDefault(command);
}
