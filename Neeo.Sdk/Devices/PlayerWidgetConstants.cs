namespace Neeo.Sdk.Devices;

internal static class PlayerWidgetConstants
{
    public const string CoverArtSensorName = "COVER_ART_SENSOR";

    public const string DescriptionSensorName = "DESCRIPTION_SENSOR";

    public const string MuteSwitchName = "MUTE";

    public const Buttons PlayerButtons = Buttons.Play | Buttons.PlayToggle | Buttons.Pause |
        Buttons.NextTrack | Buttons.PreviousTrack | Buttons.ShuffleToggle | Buttons.RepeatToggle |
        Buttons.ClearQueue | Buttons.VolumeUp | Buttons.VolumeDown | Buttons.MuteToggle;

    public const string PlayingSwitchName = "PLAYING";

    public const string QueueDirectoryName = "QUEUE_DIRECTORY";

    public const string RepeatSwitchName = "REPEAT";

    public const string RootDirectoryName = "ROOT_DIRECTORY";

    public const string ShuffleSwitchName = "SHUFFLE";

    public const string TitleSensorName = "TITLE_SENSOR";

    public const string VolumeSliderName = "VOLUME";
}