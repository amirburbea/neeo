using System;
using System.Collections.Generic;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Devices;

/// <summary>
/// An enumeration of known buttons in NEEO. Not all are explicitly recognized by the remote.
/// </summary>
/// <remarks>
/// Note: This enumeration supports bitwise (flagged) combinations for easily adding multiple buttons via a single
/// call to <see cref="IDeviceBuilder.AddButton(Buttons)"/>.
/// </remarks>
[Flags]
public enum Buttons : ulong
{
    /// <summary>
    /// &quot;BACK&quot;
    /// </summary>
    [Text("BACK")]
    Back = 1, // 0

    /// <summary>
    /// &quot;CHANNEL DOWN&quot;
    /// </summary>
    [Text("CHANNEL DOWN")]
    ChannelDown = Back << 1, // 1

    /// <summary>
    /// &quot;CHANNEL UP&quot;
    /// </summary>
    [Text("CHANNEL UP")]
    ChannelUp = ChannelDown << 1, // 2

    /// <summary>
    /// &quot;CLEAR QUEUE&quot;
    /// </summary>
    [Text("CLEAR QUEUE")]
    ClearQueue = ChannelUp << 1, // 3

    /// <summary>
    /// &quot;CURSOR DOWN&quot;
    /// </summary>
    [Text("CURSOR DOWN")]
    CursorDown = ClearQueue << 1, // 4

    /// <summary>
    /// &quot;CURSOR ENTER&quot;
    /// </summary>
    [Text("CURSOR ENTER")]
    CursorEnter = CursorDown << 1, // 5

    /// <summary>
    /// &quot;CURSOR LEFT&quot;
    /// </summary>
    [Text("CURSOR LEFT")]
    CursorLeft = CursorEnter << 1, // 6

    /// <summary>
    /// &quot;CURSOR RIGHT&quot;
    /// </summary>
    [Text("CURSOR RIGHT")]
    CursorRight = CursorLeft << 1, // 7

    /// <summary>
    /// &quot;CURSOR UP&quot;
    /// </summary>
    [Text("CURSOR UP")]
    CursorUp = CursorRight << 1, // 8

    /// <summary>
    /// &quot;DIGIT 0&quot;
    /// </summary>
    [Text("DIGIT 0")]
    Digit0 = CursorUp << 1, // 9

    /// <summary>
    /// &quot;DIGIT 1&quot;
    /// </summary>
    [Text("DIGIT 1")]
    Digit1 = Digit0 << 1, // 10

    /// <summary>
    /// &quot;DIGIT 2&quot;
    /// </summary>
    [Text("DIGIT 2")]
    Digit2 = Digit1 << 1, // 11

    /// <summary>
    /// &quot;DIGIT 3&quot;
    /// </summary>
    [Text("DIGIT 3")]
    Digit3 = Digit2 << 1, // 12

    /// <summary>
    /// &quot;DIGIT 4&quot;
    /// </summary>
    [Text("DIGIT 4")]
    Digit4 = Digit3 << 1, // 13

    /// <summary>
    /// &quot;DIGIT 5&quot;
    /// </summary>
    [Text("DIGIT 5")]
    Digit5 = Digit4 << 1, // 14

    /// <summary>
    /// &quot;DIGIT 6&quot;
    /// </summary>
    [Text("DIGIT 6")]
    Digit6 = Digit5 << 1, // 15

    /// <summary>
    /// &quot;DIGIT 7&quot;
    /// </summary>
    [Text("DIGIT 7")]
    Digit7 = Digit6 << 1, // 16

    /// <summary>
    /// &quot;DIGIT 8&quot;
    /// </summary>
    [Text("DIGIT 8")]
    Digit8 = Digit7 << 1, // 17

    /// <summary>
    /// &quot;DIGIT 9&quot;
    /// </summary>
    [Text("DIGIT 9")]
    Digit9 = Digit8 << 1, // 18

    /// <summary>
    /// &quot;DIGIT ENTER&quot;
    /// </summary>
    [Text("DIGIT ENTER")]
    DigitEnter = Digit9 << 1, // 19

    /// <summary>
    /// &quot;DIGIT SEPARATOR&quot;
    /// </summary>
    [Text("DIGIT SEPARATOR")]
    DigitSeparator = DigitEnter << 1, // 20

    /// <summary>
    /// &quot;EXIT&quot;
    /// </summary>
    [Text("EXIT")]
    Exit = DigitSeparator << 1, // 21

    /// <summary>
    /// &quot;FORWARD&quot;
    /// </summary>
    [Text("FORWARD")]
    Forward = Exit << 1, // 22

    /// <summary>
    /// &quot;FUNCTION BLUE&quot;
    /// </summary>
    [Text("FUNCTION BLUE")]
    FunctionBlue = Forward << 1, // 23

    /// <summary>
    /// &quot;FUNCTION GREEN&quot;
    /// </summary>
    [Text("FUNCTION GREEN")]
    FunctionGreen = FunctionBlue << 1, // 24

    /// <summary>
    /// &quot;FUNCTION RED&quot;
    /// </summary>
    [Text("FUNCTION RED")]
    FunctionRed = FunctionGreen << 1, // 25

    /// <summary>
    /// &quot;FUNCTION YELLOW&quot;
    /// </summary>
    [Text("FUNCTION YELLOW")]
    FunctionYellow = FunctionRed << 1, // 26

    /// <summary>
    /// &quot;GUIDE&quot;
    /// </summary>
    [Text("GUIDE")]
    Guide = FunctionYellow << 1, // 27

    /// <summary>
    /// &quot;HOME&quot;
    /// </summary>
    [Text("HOME")]
    Home = Guide << 1, // 28

    /// <summary>
    /// &quot;INFO&quot;
    /// </summary>
    [Text("INFO")]
    Info = Home << 1, // 29

    /// <summary>
    /// &quot;INPUT HDMI1&quot;
    /// </summary>
    [Text("INPUT HDMI1")]
    InputHdmi1 = Info << 1, // 30

    /// <summary>
    /// &quot;INPUT HDMI2&quot;
    /// </summary>
    [Text("INPUT HDMI2")]
    InputHdmi2 = InputHdmi1 << 1, // 31

    /// <summary>
    /// &quot;INPUT HDMI3&quot;
    /// </summary>
    [Text("INPUT HDMI3")]
    InputHdmi3 = InputHdmi2 << 1, // 32

    /// <summary>
    /// &quot;INPUT HDMI3&quot;
    /// </summary>
    [Text("INPUT HDMI4")]
    InputHdmi4 = InputHdmi3 << 1, // 33

    /// <summary>
    /// &quot;INPUT TOGGLE&quot;
    /// </summary>
    [Text("INPUT TOGGLE")]
    InputToggle = InputHdmi4 << 1, // 34

    /// <summary>
    /// &quot;LANGUAGE&quot;
    /// </summary>
    [Text("LANGUAGE")]
    Language = InputToggle << 1, // 35

    /// <summary>
    /// &quot;MENU&quot;
    /// </summary>
    [Text("MENU")]
    Menu = Language << 1, // 36

    /// <summary>
    /// &quot;MUTE TOGGLE&quot;
    /// </summary>
    [Text("MUTE TOGGLE")]
    MuteToggle = Menu << 1, // 37

    /// <summary>
    /// &quot;NEXT&quot;
    /// </summary>
    [Text("NEXT")]
    Next = MuteToggle << 1, // 38

    /// <summary>
    /// &quot;NEXT TRACK&quot;
    /// </summary>
    [Text("NEXT TRACK")]
    NextTrack = Next << 1, // 39

    /// <summary>
    /// &quot;PAUSE&quot;
    /// </summary>
    [Text("PAUSE")]
    Pause = NextTrack << 1, // 40

    /// <summary>
    /// &quot;PLAY&quot;
    /// </summary>
    [Text("PLAY")]
    Play = Pause << 1, // 41

    /// <summary>
    /// &quot;PLAY PAUSE TOGGLE&quot;
    /// </summary>
    [Text("PLAY PAUSE TOGGLE")]
    PlayPauseToggle = Play << 1, // 42

    /// <summary>
    /// &quot;PLAY TOGGLE&quot;
    /// </summary>
    [Text("PLAY TOGGLE")]
    PlayToggle = PlayPauseToggle << 1, // 43

    /// <summary>
    /// &quot;POWER OFF&quot;
    /// </summary>
    [Text("POWER OFF")]
    PowerOff = PlayToggle << 1, // 44

    /// <summary>
    /// &quot;POWER ON&quot;
    /// </summary>
    [Text("POWER ON")]
    PowerOn = PowerOff << 1, // 45

    /// <summary>
    /// &quot;POWER TOGGLE&quot;
    /// </summary>
    [Text("POWER TOGGLE")]
    PowerToggle = PowerOn << 1, // 46

    /// <summary>
    /// &quot;PREVIOUS&quot;
    /// </summary>
    [Text("PREVIOUS")]
    Previous = PowerToggle << 1, // 47

    /// <summary>
    /// &quot;RECORD&quot;
    /// </summary>
    [Text("PREVIOUS TRACK")]
    PreviousTrack = Previous << 1, // 48

    /// <summary>
    /// &quot;RECORD&quot;
    /// </summary>
    [Text("RECORD")]
    Record = PreviousTrack << 1, // 49

    /// <summary>
    /// &quot;REPEAT TOGGLE&quot;
    /// </summary>
    [Text("REPEAT TOGGLE")]
    RepeatToggle = Record << 1, // 50

    /// <summary>
    /// &quot;REVERSE&quot;
    /// </summary>
    [Text("REVERSE")]
    Reverse = RepeatToggle << 1, // 51

    /// <summary>
    /// &quot;SHUFFLE TOGGLE&quot;
    /// </summary>
    [Text("SHUFFLE TOGGLE")]
    ShuffleToggle = Reverse << 1, // 52

    /// <summary>
    /// &quot;SKIP BACKWARD&quot;
    /// </summary>
    [Text("SKIP BACKWARD")]
    SkipBackward = ShuffleToggle << 1, // 53

    /// <summary>
    /// &quot;SKIP FORWARD&quot;
    /// </summary>
    [Text("SKIP FORWARD")]
    SkipForward = SkipBackward << 1, // 54

    /// <summary>
    /// &quot;STOP&quot;
    /// </summary>
    [Text("STOP")]
    Stop = SkipForward << 1, // 55

    /// <summary>
    /// &quot;SUBTITLE&quot;
    /// </summary>
    [Text("SUBTITLE")]
    Subtitle = Stop << 1, // 56

    /// <summary>
    /// &quot;VOLUME DOWN&quot;
    /// </summary>
    [Text("VOLUME DOWN")]
    VolumeDown = Subtitle << 1, // 57

    /// <summary>
    /// &quot;VOLUME UP&quot;
    /// </summary>
    [Text("VOLUME UP")]
    VolumeUp = VolumeDown << 1 // 58
}

/// <summary>
/// Contains <see langword="static"/> methods for interacting with the <see cref="Buttons"/> enumeration.
/// </summary>
public static class Button
{
    /// <summary>
    /// Gets the button names in the specified combination of <paramref name="buttons"/>.
    /// </summary>
    /// <param name="buttons">The (potentially flagged) <see cref="Buttons"/> value.</param>
    /// <returns>The collection of button names.</returns>
    public static IEnumerable<string> GetNames(Buttons buttons) => FlaggedEnumerations.GetNames(buttons);

    /// <summary>
    /// Attempts to get the associated <see cref="Buttons"/> value for a button name.
    /// </summary>
    /// <param name="name">The name of the button.</param>
    /// <returns><see cref="Buttons"/> value if found, otherwise <see langword="null"/>.</returns>
    public static Buttons? TryResolve(string name) => TextAttribute.GetEnum<Buttons>(name);
}