using System;
using System.Collections.Generic;

namespace Remote.Neeo.Devices
{
    public enum ButtonGroup
    {
        [ButtonGroup("CHANNEL UP", "CHANNEL DOWN")]
        ChannelZapper = 0,

        [ButtonGroup("FUNCTION RED", "FUNCTION GREEN", "FUNCTION YELLOW", "FUNCTION BLUE")]
        ColorButtons,

        [ButtonGroup("CURSOR ENTER", "CURSOR UP", "CURSOR DOWN", "CURSOR LEFT", "CURSOR RIGHT")]
        ControlPad,

        [ButtonGroup("SUBTITLE", "LANGUAGE")]
        Language,

        [ButtonGroup("MENU", "BACK")]
        MenuAndBack,

        [ButtonGroup("DIGIT 0", "DIGIT 1", "DIGIT 2", "DIGIT 3", "DIGIT 4", "DIGIT 5", "DIGIT 6", "DIGIT 7", "DIGIT 8", "DIGIT 9")]
        NumberPad,

        [ButtonGroup("POWER ON", "POWER OFF")]
        Power,

        [ButtonGroup("MY RECORDINGS", "RECORD", "LIVE")]
        Record,

        [ButtonGroup("PLAY", "PAUSE", "STOP")]
        Transport,

        [ButtonGroup("PREVIOUS", "NEXT")]
        TransportScan,

        [ButtonGroup("REVERSE", "FORWARD")]
        TransportSearch,

        [ButtonGroup("SKIP SECONDS BACKWARD", "SKIP SECONDS FORWARD")]
        TransportSkip,

        [ButtonGroup("VOLUME UP", "VOLUME DOWN", "MUTE TOGGLE")]
        Volume
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class ButtonGroupAttribute : Attribute
    {
        public ButtonGroupAttribute(params string[] names) => this.Names = Array.AsReadOnly(names);

        public IReadOnlyList<string> Names { get; }

        public static IReadOnlyList<string> GetNames(ButtonGroup buttonGroup)
        {
            return AttributeData.GetEnumAttributeData(buttonGroup, (ButtonGroupAttribute attribute) => attribute.Names)!;
        }
    }
}
