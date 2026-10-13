using System.Collections.Generic;
using Neeo.Sdk.Devices;

namespace Neeo.Drivers.WebOS.Models;

public static class WebOSRemoteKey
{
    private static readonly Dictionary<Buttons, string> _mappings = new()
    {
        { Buttons.Back, "BACK" },
        { Buttons.CursorDown, "DOWN" },
        { Buttons.CursorEnter, "ENTER" },
        { Buttons.CursorLeft, "LEFT" },
        { Buttons.CursorRight, "RIGHT" },
        { Buttons.CursorUp, "UP" },
        { Buttons.Home, "HOME" },
        { Buttons.Menu, "MENU" },
        { Buttons.Info, "INFO" },
        { Buttons.Guide, "GUIDE" },
        { Buttons.Exit, "EXIT" },
        { Buttons.FunctionRed, "RED" },
        { Buttons.FunctionGreen, "GREEN" },
        { Buttons.FunctionYellow, "YELLOW" },
        { Buttons.FunctionBlue, "BLUE" },
        { Buttons.VolumeUp, "VOLUMEUP" },
        { Buttons.VolumeDown, "VOLUMEDOWN" },
        { Buttons.MuteToggle, "MUTE" },
        { Buttons.ChannelUp, "CHANNELUP" },
        { Buttons.ChannelDown, "CHANNELDOWN" },
        { Buttons.Play, "PLAY" },
        { Buttons.Pause, "PAUSE" },
        { Buttons.Stop, "STOP" },
        { Buttons.Forward, "FASTFORWARD" },
        { Buttons.Reverse, "REWIND" },
        { Buttons.Digit0, "0" },
        { Buttons.Digit1, "1" },
        { Buttons.Digit2, "2" },
        { Buttons.Digit3, "3" },
        { Buttons.Digit4, "4" },
        { Buttons.Digit5, "5" },
        { Buttons.Digit6, "6" },
        { Buttons.Digit7, "7" },
        { Buttons.Digit8, "8" },
        { Buttons.Digit9, "9" }
    };

    public static bool TryGetButtonName(Buttons button, out string buttonName) =>
        _mappings.TryGetValue(button, out buttonName!);
}
