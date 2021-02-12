using System;
using System.Collections.Generic;
using System.Linq;

namespace Remote.Neeo.Devices
{
    [Flags]
    public enum KnownButtons : ulong
    {
        [Text("BACK")]
        Back = 1ul << 0,

        [Text("CHANNEL DOWN")]
        ChannelDown = 1ul << 1,

        [Text("CHANNEL UP")]
        ChannelUp = 1ul << 2,

        [Text("CURSOR DOWN")]
        CursorDown = 1ul << 3,

        [Text("CURSOR ENTER")]
        CursorEnter = 1ul << 4,

        [Text("CURSOR LEFT")]
        CursorLeft = 1ul << 5,

        [Text("CURSOR RIGHT")]
        CursorRight = 1ul << 6,

        [Text("CURSOR UP")]
        CursorUp = 1ul << 7,

        [Text("DIGIT 0")]
        Digit0 = 1ul << 8,

        [Text("DIGIT 1")]
        Digit1 = 1ul << 9,

        [Text("DIGIT 2")]
        Digit2 = 1ul << 10,

        [Text("DIGIT 3")]
        Digit3 = 1ul << 11,

        [Text("DIGIT 4")]
        Digit4 = 1ul << 12,

        [Text("DIGIT 5")]
        Digit5 = 1ul << 13,

        [Text("DIGIT 6")]
        Digit6 = 1ul << 14,

        [Text("DIGIT 7")]
        Digit7 = 1ul << 15,

        [Text("DIGIT 8")]
        Digit8 = 1ul << 16,

        [Text("DIGIT 9")]
        Digit9 = 1ul << 17,

        [Text("DIGIT ENTER")]
        DigitEnter = 1ul << 18,

        [Text("DIGIT SEPARATOR")]
        DigitSeparator = 1ul << 19,

        [Text("EXIT")]
        Exit = 1ul << 20,

        [Text("FORWARD")]
        Forward = 1ul << 21,

        [Text("FUNCTION BLUE")]
        FunctionBlue = 1ul << 22,

        [Text("FUNCTION GREEN")]
        FunctionGreen = 1ul << 23,

        [Text("FUNCTION RED")]
        FunctionRed = 1ul << 24,

        [Text("FUNCTION YELLOW")]
        FunctionYellow = 1ul << 25,

        [Text("GUIDE")]
        Guide = 1ul << 26,

        [Text("HOME")]
        Home = 1ul << 27,

        [Text("INFO")]
        Info = 1ul << 28,

        [Text("LANGUAGE")]
        Language = 1ul << 29,

        [Text("MENU")]
        Menu = 1ul << 30,

        [Text("MUTE TOGGLE")]
        MuteToggle = 1ul << 31,

        [Text("MY RECORDINGS")] 
        MyRecordings = 1ul << 32,

        [Text("LIVE")] 
        Live = 1ul << 33,

        [Text("NEXT")] 
        Next = 1ul << 34,

        [Text("PAUSE")] 
        Pause = 1ul << 35,

        [Text("PLAY")] 
        Play = 1ul << 36,

        [Text("PLAY PAUSE TOGGLE")] 
        PlayPauseToggle = 1ul << 37,

        [Text("POWER OFF")] 
        PowerOff = 1ul << 38,

        [Text("POWER ON")] 
        PowerOn = 1ul << 39,

        [Text("POWER TOGGLE")] 
        PowerToggle = 1ul << 40,

        [Text("PREVIOUS")] 
        Previous = 1ul << 41,

        [Text("RECORD")] 
        Record = 1ul << 42,

        [Text("REVERSE")] 
        Reverse = 1ul << 43,

        [Text("SKIP BACKWARD")] 
        SkipBackward = 1ul << 44,

        [Text("SKIP FORWARD")] 
        SkipSForward = 1ul << 45,

        [Text("SKIP SECONDS BACKWARD")] 
        SkipSecondsBackward = 1ul << 46,

        [Text("SKIP SECONDS FORWARD")] 
        SkipSecondsForward = 1ul << 47,

        [Text("STOP")] Stop = 
            1ul << 48,

        [Text("SUBTITLE")] 
        Subtitle = 1ul << 49,

        [Text("VOLUME DOWN")] 
        VolumeDown = 1ul << 50,

        [Text("VOLUME UP")] 
        VolumeUp = 1ul << 51,
    }

    /// <summary>
    /// Contains <see langword="static"/> methods for interacting with the <see cref="KnownButtons"/> enumeration.
    /// </summary>
    public static class KnownButton
    {
        public static IEnumerable<string> GetNames(KnownButtons buttons)
        {
            ulong value = (ulong)buttons;
            if (value == 0ul)
            {
                return Enumerable.Empty<string>();
            }
            bool isSingleBit = (value & (value - 1ul)) == 0ul;
            return isSingleBit
                ? new[] { TextAttribute.GetEnumText(buttons) }
                : KnownButton.ExtractBits(value).Select(bit => TextAttribute.GetEnumText((KnownButtons)bit));
        }

        public static KnownButtons? TryGetKnownButton(string name) => TextAttribute.GetEnum<KnownButtons>(name);

        private static IEnumerable<ulong> ExtractBits(ulong value)
        {
            for (int i = 0; i < 64; i++)
            {
                ulong flag = 1ul << i;
                if (flag > value)
                {
                    yield break;
                }
                if ((value & flag) == flag)
                {
                    yield return flag;
                }
            }
        }
    }
}
