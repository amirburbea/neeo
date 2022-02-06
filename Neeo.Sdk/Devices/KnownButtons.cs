using System;
using System.Collections.Generic;
using System.Linq;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

/// <summary>
/// An enumeration of known buttons in NEEO.
/// Not all are explicitly recognized by the remote.
/// </summary>
/// <remarks>
/// Note: This enumeration supports bitwise (flagged) combinations for easily adding multiple buttons via a single
/// call to <see cref="IDeviceBuilder.AddButton(KnownButtons)"/>.
/// </remarks>
[Flags]
public enum KnownButtons : ulong
{
    /// <summary>
    /// &quot;AMAZON&quot;
    /// </summary>
    [Text("AMAZON")]
    Amazon = 1UL,

    /// <summary>
    /// &quot;BACK&quot;
    /// </summary>
    [Text("BACK")]
    Back = Amazon << 1,

    /// <summary>
    /// &quot;CHANNEL DOWN&quot;
    /// </summary>
    [Text("CHANNEL DOWN")]
    ChannelDown = Back << 1,

    /// <summary>
    /// &quot;CHANNEL UP&quot;
    /// </summary>
    [Text("CHANNEL UP")]
    ChannelUp = ChannelDown << 1,

    /// <summary>
    /// &quot;CLEAR QUEUE&quot;
    /// </summary>
    [Text("CLEAR QUEUE")]
    ClearQueue = ChannelUp << 1,

    /// <summary>
    /// &quot;CURSOR DOWN&quot;
    /// </summary>
    [Text("CURSOR DOWN")]
    CursorDown = ClearQueue << 1,

    /// <summary>
    /// &quot;CURSOR ENTER&quot;
    /// </summary>
    [Text("CURSOR ENTER")]
    CursorEnter = CursorDown << 1,

    /// <summary>
    /// &quot;CURSOR LEFT&quot;
    /// </summary>
    [Text("CURSOR LEFT")]
    CursorLeft = CursorEnter << 1,

    /// <summary>
    /// &quot;CURSOR RIGHT&quot;
    /// </summary>
    [Text("CURSOR RIGHT")]
    CursorRight = CursorLeft << 1,

    /// <summary>
    /// &quot;CURSOR UP&quot;
    /// </summary>
    [Text("CURSOR UP")]
    CursorUp = CursorRight << 1,

    /// <summary>
    /// &quot;DIGIT 0&quot;
    /// </summary>
    [Text("DIGIT 0")]
    Digit0 = CursorUp << 1,

    /// <summary>
    /// &quot;DIGIT 1&quot;
    /// </summary>
    [Text("DIGIT 1")]
    Digit1 = Digit0 << 1,

    /// <summary>
    /// &quot;DIGIT 2&quot;
    /// </summary>
    [Text("DIGIT 2")]
    Digit2 = Digit1 << 1,

    /// <summary>
    /// &quot;DIGIT 3&quot;
    /// </summary>
    [Text("DIGIT 3")]
    Digit3 = Digit2 << 1,

    /// <summary>
    /// &quot;DIGIT 4&quot;
    /// </summary>
    [Text("DIGIT 4")]
    Digit4 = Digit3 << 1,

    /// <summary>
    /// &quot;DIGIT 5&quot;
    /// </summary>
    [Text("DIGIT 5")]
    Digit5 = Digit4 << 1,

    /// <summary>
    /// &quot;DIGIT 6&quot;
    /// </summary>
    [Text("DIGIT 6")]
    Digit6 = Digit5 << 1,

    /// <summary>
    /// &quot;DIGIT 7&quot;
    /// </summary>
    [Text("DIGIT 7")]
    Digit7 = Digit6 << 1,

    /// <summary>
    /// &quot;DIGIT 8&quot;
    /// </summary>
    [Text("DIGIT 8")]
    Digit8 = Digit7 << 1,

    /// <summary>
    /// &quot;DIGIT 9&quot;
    /// </summary>
    [Text("DIGIT 9")]
    Digit9 = Digit8 << 1,

    /// <summary>
    /// &quot;DIGIT ENTER&quot;
    /// </summary>
    [Text("DIGIT ENTER")]
    DigitEnter = Digit9 << 1,

    /// <summary>
    /// &quot;DIGIT SEPARATOR&quot;
    /// </summary>
    [Text("DIGIT SEPARATOR")]
    DigitSeparator = DigitEnter << 1,

    /// <summary>
    /// &quot;EXIT&quot;
    /// </summary>
    [Text("EXIT")]
    Exit = DigitSeparator << 1,

    /// <summary>
    /// &quot;FORWARD&quot;
    /// </summary>
    [Text("FORWARD")]
    Forward = Exit << 1,

    /// <summary>
    /// &quot;FUNCTION BLUE&quot;
    /// </summary>
    [Text("FUNCTION BLUE")]
    FunctionBlue = Forward << 1,

    /// <summary>
    /// &quot;FUNCTION GREEN&quot;
    /// </summary>
    [Text("FUNCTION GREEN")]
    FunctionGreen = FunctionBlue << 1,

    /// <summary>
    /// &quot;FUNCTION RED&quot;
    /// </summary>
    [Text("FUNCTION RED")]
    FunctionRed = FunctionGreen << 1,

    /// <summary>
    /// &quot;FUNCTION YELLOW&quot;
    /// </summary>
    [Text("FUNCTION YELLOW")]
    FunctionYellow = FunctionRed << 1,

    /// <summary>
    /// &quot;GUIDE&quot;
    /// </summary>
    [Text("GUIDE")]
    Guide = FunctionYellow << 1,

    /// <summary>
    /// &quot;HOME&quot;
    /// </summary>
    [Text("HOME")]
    Home = Guide << 1,

    /// <summary>
    /// &quot;INFO&quot;
    /// </summary>
    [Text("INFO")]
    Info = Home << 1,

    /// <summary>
    /// &quot;INPUT HDMI1&quot;
    /// </summary>
    [Text("INPUT HDMI1")]
    InputHdmi1 = Info << 1,

    /// <summary>
    /// &quot;INPUT HDMI2&quot;
    /// </summary>
    [Text("INPUT HDMI2")]
    InputHdmi2 = InputHdmi1 << 1,

    /// <summary>
    /// &quot;INPUT HDMI3&quot;
    /// </summary>
    [Text("INPUT HDMI3")]
    InputHdmi3 = InputHdmi2 << 1,

    /// <summary>
    /// &quot;INPUT HDMI3&quot;
    /// </summary>
    [Text("INPUT HDMI4")]
    InputHdmi4 = InputHdmi3 << 1,

    /// <summary>
    /// &quot;INPUT TOGGLE&quot;
    /// </summary>
    [Text("INPUT TOGGLE")]
    InputToggle = InputHdmi4 << 1,

    /// <summary>
    /// &quot;MENU&quot;
    /// </summary>
    [Text("MENU")]
    Menu = InputToggle << 1,

    /// <summary>
    /// &quot;MUTE TOGGLE&quot;
    /// </summary>
    [Text("MUTE TOGGLE")]
    MuteToggle = Menu << 1,

     /// <summary>
    /// &quot;NETFLIX&quot;
    /// </summary>
    [Text("NETFLIX")]
    Netflix = MuteToggle << 1,

    /// <summary>
    /// &quot;NEXT&quot;
    /// </summary>
    [Text("NEXT")]
    Next = Netflix << 1,

    /// <summary>
    /// &quot;NEXT TRACK&quot;
    /// </summary>
    [Text("NEXT TRACK")]
    NextTrack = Next << 1,

    /// <summary>
    /// &quot;PAUSE&quot;
    /// </summary>
    [Text("PAUSE")]
    Pause = NextTrack << 1,

    /// <summary>
    /// &quot;PLAY&quot;
    /// </summary>
    [Text("PLAY")]
    Play = Pause << 1,

    /// <summary>
    /// &quot;PLAY PAUSE TOGGLE&quot;
    /// </summary>
    [Text("PLAY PAUSE TOGGLE")]
    PlayPauseToggle = Play << 1,

    /// <summary>
    /// &quot;POWER OFF&quot;
    /// </summary>
    [Text("POWER OFF")]
    PowerOff = PlayPauseToggle << 1,

    /// <summary>
    /// &quot;POWER ON&quot;
    /// </summary>
    [Text("POWER ON")]
    PowerOn = PowerOff << 1,

    /// <summary>
    /// &quot;POWER TOGGLE&quot;
    /// </summary>
    [Text("POWER TOGGLE")]
    PowerToggle = PowerOn << 1,

    /// <summary>
    /// &quot;PREVIOUS&quot;
    /// </summary>
    [Text("PREVIOUS")]
    Previous = PowerToggle << 1,

    /// <summary>
    /// &quot;RECORD&quot;
    /// </summary>
    [Text("PREVIOUS TRACK")]
    PreviousTrack = Previous << 1,

    /// <summary>
    /// &quot;RECORD&quot;
    /// </summary>
    [Text("RECORD")]
    Record = PreviousTrack << 1,

    /// <summary>
    /// &quot;REPEAT TOGGLE&quot;
    /// </summary>
    [Text("REPEAT TOGGLE")]
    RepeatToggle = Record << 1,

    /// <summary>
    /// &quot;REVERSE&quot;
    /// </summary>
    [Text("REVERSE")]
    Reverse = RepeatToggle << 1,

    /// <summary>
    /// &quot;SHUFFLE TOGGLE&quot;
    /// </summary>
    [Text("SHUFFLE TOGGLE")]
    ShuffleToggle = Reverse << 1,

    /// <summary>
    /// &quot;SKIP BACKWARD&quot;
    /// </summary>
    [Text("SKIP BACKWARD")]
    SkipBackward = ShuffleToggle << 1,

    /// <summary>
    /// &quot;SKIP FORWARD&quot;
    /// </summary>
    [Text("SKIP FORWARD")]
    SkipForward = SkipBackward << 1,

    /// <summary>
    /// &quot;STOP&quot;
    /// </summary>
    [Text("STOP")]
    Stop = SkipForward << 1,

    /// <summary>
    /// &quot;SUBTITLE&quot;
    /// </summary>
    [Text("SUBTITLE")]
    Subtitle = Stop << 1,

    /// <summary>
    /// &quot;VOLUME DOWN&quot;
    /// </summary>
    [Text("VOLUME DOWN")]
    VolumeDown = Subtitle << 1,

    /// <summary>
    /// &quot;VOLUME UP&quot;
    /// </summary>
    [Text("VOLUME UP")]
    VolumeUp = VolumeDown << 1,

    /// <summary>
    /// &quot;YOU TUBE&quot;
    /// </summary>
    [Text("YOU TUBE")]
    YouTube = VolumeUp << 1,
}

/// <summary>
/// Contains <see langword="static"/> methods for interacting with the <see cref="KnownButtons"/> enumeration.
/// </summary>
public static class KnownButton
{
    /// <summary>
    /// Attempts to get the associated <see cref="KnownButtons"/> value for a button name.
    /// </summary>
    /// <param name="name">The name of the button.</param>
    /// <returns><see cref="KnownButtons"/> value if found, otherwise <c>null</c>.</returns>
    public static KnownButtons? TryGetKnownButton(string name) => TextAttribute.GetEnum<KnownButtons>(name);

    /// <summary>
    /// Gets the button names in the specified combination of <paramref name="buttons"/>.
    /// </summary>
    /// <param name="buttons">The (potentially flagged) <see cref="KnownButtons"/> value.</param>
    /// <returns>The collection of button names.</returns>
    public static IEnumerable<string> GetNames(KnownButtons buttons)
    {
        ulong value = (ulong)buttons;
        if (value == default)
        {
            return Enumerable.Empty<string>();
        }
        bool isSingleFlag = (value & (value - 1ul)) == default;
        return isSingleFlag
            ? new[] { TextAttribute.GetText(buttons) }
            : KnownButton.ExtractFlags(value);
    }

    private static IEnumerable<string> ExtractFlags(ulong value)
    {
        for (int bits = 0; bits < 64; bits++)
        {
            ulong flag = 1ul << bits;
            if (flag > value)
            {
                yield break;
            }
            if ((value & flag) == flag)
            {
                yield return TextAttribute.GetText((KnownButtons)flag);
            }
        }
    }
}