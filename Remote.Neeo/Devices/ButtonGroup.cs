namespace Remote.Neeo.Devices
{
    public enum ButtonGroup : ulong
    {
        ChannelZapper = KnownButtons.ChannelDown | KnownButtons.ChannelUp,

        ColorButtons = KnownButtons.FunctionBlue | KnownButtons.FunctionGreen | KnownButtons.FunctionRed | KnownButtons.FunctionYellow,

        ControlPad = KnownButtons.CursorDown | KnownButtons.CursorEnter | KnownButtons.CursorLeft | KnownButtons.CursorRight | KnownButtons.CursorUp,

        Language = KnownButtons.Subtitle | KnownButtons.Language,

        MenuAndBack = KnownButtons.Menu | KnownButtons.Back,

        NumberPad = KnownButtons.Digit0 | KnownButtons.Digit1 | KnownButtons.Digit2 | KnownButtons.Digit3 | KnownButtons.Digit4 | KnownButtons.Digit5 | KnownButtons.Digit6 | KnownButtons.Digit7 | KnownButtons.Digit8 | KnownButtons.Digit9,

        Power = KnownButtons.PowerOn | KnownButtons.PowerOff,

        Record = KnownButtons.MyRecordings | KnownButtons.Record | KnownButtons.Live,

        Transport = KnownButtons.Play | KnownButtons.Pause | KnownButtons.Stop,

        TransportScan = KnownButtons.Previous | KnownButtons.Next,

        TransportSearch = KnownButtons.Reverse | KnownButtons.Forward,

        TransportSkip = KnownButtons.SkipSecondsBackward | KnownButtons.SkipSecondsForward,

        Volume = KnownButtons.VolumeUp | KnownButtons.VolumeDown | KnownButtons.MuteToggle
    }
}
