namespace Neeo.Sdk.Devices;

using System;
using static KnownButtons;

/// <summary>
/// Groups of buttons which can be added via a single call to <see cref="IDeviceBuilder.AddButtonGroup"/>.
/// </summary>
[Flags]
public enum ButtonGroups : ulong
{
    /// <summary>
    /// &quot;CHANNEL DOWN&quot;, &quot;CHANNEL UP&quot;.
    /// </summary>
    ChannelZapper = ChannelDown | ChannelUp,

    /// <summary>
    /// &quot;FUNCTION BLUE&quot;, &quot;FUNCTION GREEN&quot;, &quot;FUNCTION RED&quot;,
    /// &quot;FUNCTION YELLOW&quot;.
    /// </summary>
    ColorButtons = FunctionBlue | FunctionGreen | FunctionRed | FunctionYellow,

    /// <summary>
    /// &quot;CURSOR DOWN&quot;, &quot;CURSOR ENTER&quot;, &quot;CURSOR LEFT&quot;, &quot;CURSOR RIGHT&quot;,
    /// &quot;CURSOR UP&quot;.
    /// </summary>
    ControlPad = CursorDown | CursorEnter | CursorLeft | CursorRight | CursorUp,

    /// <summary>
    /// &quot;SUBTITLE&quot;, &quot;LANGUAGE&quot;.
    /// </summary>
    Locale = Subtitle | Language,

    /// <summary>
    /// &quot;MENU&quot;, &quot;BACK&quot;.
    /// </summary>
    MenuAndBack = Menu | Back,

    /// <summary>
    /// &quot;DIGIT 0&quot;, &quot;DIGIT 1&quot;, &quot;DIGIT 2&quot;, &quot;DIGIT 3&quot;, &quot;DIGIT 4&quot;, 
    /// &quot;DIGIT 5&quot;, &quot;DIGIT 6&quot;, &quot;DIGIT 7&quot;, &quot;DIGIT 8&quot;, &quot;DIGIT 9&quot;.
    /// </summary>
    NumberPad = Digit0 | Digit1 | Digit2 | Digit3 | Digit4 | Digit5 | Digit6 | Digit7 | Digit8 | Digit9,

    /// <summary>
    /// &quot;POWER ON&quot;, &quot;POWER OFF&quot;.
    /// </summary>
    Power = PowerOn | PowerOff,

    /// <summary>
    /// &quot;MY RECORDINGS&quot;, &quot;RECORD&quot;, &quot;LIVE&quot;.
    /// </summary>
    Recording = MyRecordings | Record | Live,

    /// <summary>
    /// &quot;PLAY&quot;, &quot;PAUSE&quot;, &quot;STOP&quot;.
    /// </summary>
    Transport = Play | Pause | Stop,

    /// <summary>
    /// &quot;PREVIOUS&quot;, &quot;NEXT&quot;.
    /// </summary>
    TransportScan = Previous | Next,

    /// <summary>
    /// &quot;REVERSE&quot;, &quot;FORWARD&quot;.
    /// </summary>
    TransportSearch = Reverse | Forward,

    /// <summary>
    /// &quot;SKIP SECONDS BACKWARD&quot;, &quot;SKIP SECONDS FORWARD&quot;.
    /// </summary>
    TransportSkip = SkipSecondsBackward | SkipSecondsForward,

    /// <summary>
    /// &quot;VOLUME UP&quot;, &quot;VOLUME DOWN&quot;, &quot;MUTE TOGGLE&quot;.
    /// </summary>
    Volume = VolumeUp | VolumeDown | MuteToggle,
}
