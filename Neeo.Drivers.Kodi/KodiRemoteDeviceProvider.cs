using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;

namespace Neeo.Drivers.Kodi;

public sealed class KodiRemoteDeviceProvider : KodiDeviceProviderBase
{
    public KodiRemoteDeviceProvider(KodiClientManager clientManager, ILogger<KodiRemoteDeviceProvider> logger)
        : base(clientManager, "Remote (Kodi)", DeviceType.TV, logger)
    {
    }

    protected override IDeviceBuilder CreateDevice() => base.CreateDevice()
        .AddTextLabel("TITLE", "Now Playing Title", this.GetTitleAsync, isLabelVisible: true)
        .AddTextLabel("DESCRIPTION", "Now Playing Description", this.GetDescriptionAsync, isLabelVisible: true)
        .AddImageUrl("COVER_ART", "Cover Art", ImageSize.Large, getter: this.GetCoverArtAsync)
        .AddSensor("PLAYING_SENSOR", default, this.GetIsPlayingAsync)
        .AddButtonGroup(ButtonGroups.Power | ButtonGroups.ChannelZapper | ButtonGroups.Transport | ButtonGroups.Volume | ButtonGroups.NumberPad | ButtonGroups.ControlPad | ButtonGroups.MenuAndBack)
        .AddButton("CHANNEL SEARCH")
        .AddButton("DIGIT SEPARATOR")
        .AddButton("FAVORITE")
        .AddButton("OSD")
        .AddButton("PLAY PAUSE")
        .AddButton("Scan Video Library")
        .AddButton("Scan Audio Library")
        .AddButton("TOGGLE FULLSCREEN")
        .AddButton(Buttons.SkipBackward | Buttons.SkipForward | Buttons.Exit | Buttons.Forward | Buttons.Previous | Buttons.PreviousTrack | Buttons.Next | Buttons.NextTrack | Buttons.Home | Buttons.Reverse | Buttons.PlayPauseToggle | Buttons.Info | Buttons.Subtitle)
        .AddButton("INPUT KODI", "Kodi"); // Need an Input for TV so we just pretend there's a generic input called KODI.
}