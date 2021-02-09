using System;
using System.Collections.Generic;

namespace Remote.Neeo.Devices
{
    public enum ButtonGroup
    {
        /// <summary>
        /// &quot;CHANNEL UP&quot; and &quot;CHANNEL DOWN&quot;.
        /// </summary>
        [ButtonGroup("CHANNEL UP", "CHANNEL DOWN")]
        ChannelZapper = 0,

        /// <summary>
        /// &quot;FUNCTION RED&quot;, &quot;FUNCTION GREEN&quot;, &quot;FUNCTION YELLOW&quot; and &quot;FUNCTION BLUE&quot;.
        /// </summary>
        [ButtonGroup("FUNCTION RED", "FUNCTION GREEN", "FUNCTION YELLOW", "FUNCTION BLUE")]
        ColorButtons,

        /// <summary>
        /// &quot;CURSOR ENTER&quot;, &quot;CURSOR UP&quot;, &quot;CURSOR DOWN&quot;, &quot;CURSOR LEFT&quot;, and &quot;CURSOR RIGHT&quot;.
        /// </summary>
        [ButtonGroup("CURSOR ENTER", "CURSOR UP", "CURSOR DOWN", "CURSOR LEFT", "CURSOR RIGHT")]
        ControlPad,

        /// <summary>
        /// &quot;SUBTITLE&quot; and &quot;LANGUAGE&quot;.
        /// </summary>
        [ButtonGroup("SUBTITLE", "LANGUAGE")]
        Language,

        /// <summary>
        /// &quot;MENU&quot; and &quot;BACK&quot;.
        /// </summary>
        [ButtonGroup("MENU", "BACK")]
        MenuAndBack,

        /// <summary>
        /// &quot;DIGIT 0&quot;, &quot;DIGIT 1&quot;, &quot;DIGIT 2&quot;, &quot;DIGIT 3&quot;, &quot;DIGIT 4&quot;, &quot;DIGIT 5&quot;, &quot;DIGIT 6&quot;, &quot;DIGIT 7&quot;, &quot;DIGIT 8&quot;, and &quot;DIGIT 9&quot;.
        /// </summary>
        [ButtonGroup("DIGIT 0", "DIGIT 1", "DIGIT 2", "DIGIT 3", "DIGIT 4", "DIGIT 5", "DIGIT 6", "DIGIT 7", "DIGIT 8", "DIGIT 9")]
        NumberPad,

        /// <summary>
        /// &quot;POWER ON&quot; and &quot;POWER OFF&quot;.
        /// </summary>
        [ButtonGroup("POWER ON", "POWER OFF")]
        Power,

        /// <summary>
        /// &quot;MY RECORDINGS&quot;, &quot;RECORD&quot;, and &quot;LIVE&quot;.
        /// </summary>
        [ButtonGroup("MY RECORDINGS", "RECORD", "LIVE")]
        Record,

        /// <summary>
        /// &quot;PLAY&quot;, &quot;PAUSE&quot;, and &quot;STOP&quot;.
        /// </summary>
        [ButtonGroup("PLAY", "PAUSE", "STOP")]
        Transport,

        /// <summary>
        /// &quot;PREVIOUS&quot; and &quot;NEXT&quot;.
        /// </summary>
        [ButtonGroup("PREVIOUS", "NEXT")]
        TransportScan,

        /// <summary>
        /// &quot;REVERSE&quot; and &quot;FORWARD&quot;.
        /// </summary>
        [ButtonGroup("REVERSE", "FORWARD")]
        TransportSearch,

        /// <summary>
        /// &quot;SKIP SECONDS BACKWARD&quot; and &quot;SKIP SECONDS FORWARD&quot;.
        /// </summary>
        [ButtonGroup("SKIP SECONDS BACKWARD", "SKIP SECONDS FORWARD")]
        TransportSkip,

        /// <summary>
        /// &quot;VOLUME UP&quot;, &quot;VOLUME DOWN&quot; and &quot;MUTE TOGGLE&quot;.
        /// </summary>
        [ButtonGroup("VOLUME UP", "VOLUME DOWN", "MUTE TOGGLE")]
        Volume
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    internal sealed class ButtonGroupAttribute : Attribute
    {
        public ButtonGroupAttribute(params string[] names) => this.Names = Array.AsReadOnly(names);

        public IReadOnlyList<string> Names { get; }

        public static IReadOnlyList<string> GetNames(ButtonGroup buttonGroup)
        {
            return AttributeData.GetEnumAttributeData(buttonGroup, (ButtonGroupAttribute attribute) => attribute.Names)!;
        }
    }
}
