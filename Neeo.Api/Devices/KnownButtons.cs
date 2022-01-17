using System;
using System.Collections.Generic;
using System.Linq;

namespace Neeo.Api.Devices;

/// <summary>
/// An enumeration of known buttons in NEEO.
/// Not all are explicitly recognized by the remote.
/// </summary>
/// <remarks>
/// Note: This enumeration supports bitwise (flagged) combinations for easily adding multiple buttons via a single
/// call to <see cref="IDeviceBuilder.AddButtons(KnownButtons)"/>.
/// </remarks>
[Flags]
public enum KnownButtons : ulong
{
    /// <summary>
    /// &quot;BACK&quot;
    /// </summary>
    [Text("BACK")]
    Back = 1ul << 0,

    /// <summary>
    /// &quot;CHANNEL DOWN&quot;
    /// </summary>
    [Text("CHANNEL DOWN")]
    ChannelDown = 1ul << 1,

    /// <summary>
    /// &quot;CHANNEL UP&quot;
    /// </summary>
    [Text("CHANNEL UP")]
    ChannelUp = 1ul << 2,

    /// <summary>
    /// &quot;CURSOR DOWN&quot;
    /// </summary>
    [Text("CURSOR DOWN")]
    CursorDown = 1ul << 3,

    /// <summary>
    /// &quot;CURSOR ENTER&quot;
    /// </summary>
    [Text("CURSOR ENTER")]
    CursorEnter = 1ul << 4,

    /// <summary>
    /// &quot;CURSOR LEFT&quot;
    /// </summary>
    [Text("CURSOR LEFT")]
    CursorLeft = 1ul << 5,

    /// <summary>
    /// &quot;CURSOR RIGHT&quot;
    /// </summary>
    [Text("CURSOR RIGHT")]
    CursorRight = 1ul << 6,

    /// <summary>
    /// &quot;CURSOR UP&quot;
    /// </summary>
    [Text("CURSOR UP")]
    CursorUp = 1ul << 7,

    /// <summary>
    /// &quot;DIGIT 0&quot;
    /// </summary>
    [Text("DIGIT 0")]
    Digit0 = 1ul << 8,

    /// <summary>
    /// &quot;DIGIT 1&quot;
    /// </summary>
    [Text("DIGIT 1")]
    Digit1 = 1ul << 9,

    /// <summary>
    /// &quot;DIGIT 2&quot;
    /// </summary>
    [Text("DIGIT 2")]
    Digit2 = 1ul << 10,

    /// <summary>
    /// &quot;DIGIT 3&quot;
    /// </summary>
    [Text("DIGIT 3")]
    Digit3 = 1ul << 11,

    /// <summary>
    /// &quot;DIGIT 4&quot;
    /// </summary>
    [Text("DIGIT 4")]
    Digit4 = 1ul << 12,

    /// <summary>
    /// &quot;DIGIT 5&quot;
    /// </summary>
    [Text("DIGIT 5")]
    Digit5 = 1ul << 13,

    /// <summary>
    /// &quot;DIGIT 6&quot;
    /// </summary>
    [Text("DIGIT 6")]
    Digit6 = 1ul << 14,

    /// <summary>
    /// &quot;DIGIT 7&quot;
    /// </summary>
    [Text("DIGIT 7")]
    Digit7 = 1ul << 15,

    /// <summary>
    /// &quot;DIGIT 8&quot;
    /// </summary>
    [Text("DIGIT 8")]
    Digit8 = 1ul << 16,

    /// <summary>
    /// &quot;DIGIT 9&quot;
    /// </summary>
    [Text("DIGIT 9")]
    Digit9 = 1ul << 17,

    /// <summary>
    /// &quot;DIGIT ENTER&quot;
    /// </summary>
    [Text("DIGIT ENTER")]
    DigitEnter = 1ul << 18,

    /// <summary>
    /// &quot;DIGIT SEPARATOR&quot;
    /// </summary>
    [Text("DIGIT SEPARATOR")]
    DigitSeparator = 1ul << 19,

    /// <summary>
    /// &quot;EXIT&quot;
    /// </summary>
    [Text("EXIT")]
    Exit = 1ul << 20,

    /// <summary>
    /// &quot;FORMWARD&quot;
    /// </summary>
    [Text("FORWARD")]
    Forward = 1ul << 21,

    /// <summary>
    /// &quot;FUNCTION BLUE&quot;
    /// </summary>
    [Text("FUNCTION BLUE")]
    FunctionBlue = 1ul << 22,

    /// <summary>
    /// &quot;FUNCTION GREEN&quot;
    /// </summary>
    [Text("FUNCTION GREEN")]
    FunctionGreen = 1ul << 23,

    /// <summary>
    /// &quot;FUNCTION RED&quot;
    /// </summary>
    [Text("FUNCTION RED")]
    FunctionRed = 1ul << 24,

    /// <summary>
    /// &quot;FUNCTION YELLOW&quot;
    /// </summary>
    [Text("FUNCTION YELLOW")]
    FunctionYellow = 1ul << 25,

    /// <summary>
    /// &quot;GUIDE&quot;
    /// </summary>
    [Text("GUIDE")]
    Guide = 1ul << 26,

    /// <summary>
    /// &quot;HOME&quot;
    /// </summary>
    [Text("HOME")]
    Home = 1ul << 27,

    /// <summary>
    /// &quot;INFO&quot;
    /// </summary>
    [Text("INFO")]
    Info = 1ul << 28,

    /// <summary>
    /// &quot;LANGUAGE&quot;
    /// </summary>
    [Text("LANGUAGE")]
    Language = 1ul << 29,

    /// <summary>
    /// &quot;MENU&quot;
    /// </summary>
    [Text("MENU")]
    Menu = 1ul << 30,

    /// <summary>
    /// &quot;MUTE TOGGLE&quot;
    /// </summary>
    [Text("MUTE TOGGLE")]
    MuteToggle = 1ul << 31,

    /// <summary>
    /// &quot;MY RECORDINGS&quot;
    /// </summary>
    [Text("MY RECORDINGS")]
    MyRecordings = 1ul << 32,

    /// <summary>
    /// &quot;LIVE&quot;
    /// </summary>
    [Text("LIVE")]
    Live = 1ul << 33,

    /// <summary>
    /// &quot;NEXT&quot;
    /// </summary>
    [Text("NEXT")]
    Next = 1ul << 34,

    /// <summary>
    /// &quot;PAUSE&quot;
    /// </summary>
    [Text("PAUSE")]
    Pause = 1ul << 35,

    /// <summary>
    /// &quot;PLAY&quot;
    /// </summary>
    [Text("PLAY")]
    Play = 1ul << 36,

    /// <summary>
    /// &quot;PLAY PAUSE TOGGLE&quot;
    /// </summary>
    [Text("PLAY PAUSE TOGGLE")]
    PlayPauseToggle = 1ul << 37,

    /// <summary>
    /// &quot;POWER OFF&quot;
    /// </summary>
    [Text("POWER OFF")]
    PowerOff = 1ul << 38,

    /// <summary>
    /// &quot;POWER ON&quot;
    /// </summary>
    [Text("POWER ON")]
    PowerOn = 1ul << 39,

    /// <summary>
    /// &quot;POWER TOGGLE&quot;
    /// </summary>
    [Text("POWER TOGGLE")]
    PowerToggle = 1ul << 40,

    /// <summary>
    /// &quot;PREVIOUS&quot;
    /// </summary>
    [Text("PREVIOUS")]
    Previous = 1ul << 41,

    /// <summary>
    /// &quot;RECORD&quot;
    /// </summary>
    [Text("RECORD")]
    Record = 1ul << 42,

    /// <summary>
    /// &quot;REVERSE&quot;
    /// </summary>
    [Text("REVERSE")]
    Reverse = 1ul << 43,

    /// <summary>
    /// &quot;SKIP BACKWARD&quot;
    /// </summary>
    [Text("SKIP BACKWARD")]
    SkipBackward = 1ul << 44,

    /// <summary>
    /// &quot;SKIP FORWARD&quot;
    /// </summary>
    [Text("SKIP FORWARD")]
    SkipSForward = 1ul << 45,

    /// <summary>
    /// &quot;SKIP SECONDS BACKWARD&quot;
    /// </summary>
    [Text("SKIP SECONDS BACKWARD")]
    SkipSecondsBackward = 1ul << 46,

    /// <summary>
    /// &quot;SKIP SECONDS FORWARD&quot;
    /// </summary>
    [Text("SKIP SECONDS FORWARD")]
    SkipSecondsForward = 1ul << 47,

    /// <summary>
    /// &quot;STOP&quot;
    /// </summary>
    [Text("STOP")]
    Stop = 1ul << 48,

    /// <summary>
    /// &quot;SUBTITLE&quot;
    /// </summary>
    [Text("SUBTITLE")]
    Subtitle = 1ul << 49,

    /// <summary>
    /// &quot;VOLUME DOWN&quot;
    /// </summary>
    [Text("VOLUME DOWN")]
    VolumeDown = 1ul << 50,

    /// <summary>
    /// &quot;VOLUME UP&quot;
    /// </summary>
    [Text("VOLUME UP")]
    VolumeUp = 1ul << 51,
}

/// <summary>
/// Contains <see langword="static"/> methods for interacting with the <see cref="KnownButtons"/> enumeration.
/// </summary>
public static class KnownButton
{
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

    /// <summary>
    /// Attempts to get the associated <see cref="KnownButtons"/> value for a button name.
    /// </summary>
    /// <param name="name">The name of the button.</param>
    /// <returns><see cref="KnownButtons"/> value if found, otherwise <c>null</c>.</returns>
    public static KnownButtons? TryGetKnownButton(string name) => TextAttribute.GetEnum<KnownButtons>(name);

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
