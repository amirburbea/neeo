using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;

namespace Neeo.Drivers.Kodi;

public sealed class KodiRemoteDeviceProvider(KodiClientManager clientManager, ILogger<KodiRemoteDeviceProvider> logger) : KodiDeviceProviderBase(clientManager, "Remote", DeviceType.TV, logger)
{
    protected override IDeviceBuilder CreateDevice() => base.CreateDevice()
        .AddTextLabel("TITLE", "Now Playing Title", (deviceId, _) => this.GetTitle(deviceId), isLabelVisible: true)
        .AddTextLabel("DESCRIPTION", "Now Playing Description", (deviceId, _) => this.GetDescription(deviceId), isLabelVisible: true)
        .AddImageUrl("COVER_ART", "Cover Art", ImageSize.Large, (deviceId, _) => this.GetCoverArt(deviceId))
        .AddSensor("PLAYING_SENSOR", default, (deviceId, _) => this.GetIsPlaying(deviceId))
        .AddButtonGroup(ButtonGroups.Power | ButtonGroups.ChannelZapper | ButtonGroups.Transport | ButtonGroups.Volume | ButtonGroups.NumberPad | ButtonGroups.ControlPad | ButtonGroups.MenuAndBack)
        .AddButton("CHANNEL SEARCH")
        .AddButton("DIGIT SEPARATOR")
        .AddButton("FAVORITE")
        .AddButton("OSD")
        .AddButton("PLAY PAUSE")
        .AddButton("Scan Video Library")
        .AddButton("Scan Audio Library")
        .AddButton("TOGGLE FULLSCREEN")
        .AddSlider("VOLUME", default, (deviceId, _) => this.GetVolume(deviceId), this.SetVolumeAsync)
        .AddButton(Buttons.SkipBackward | Buttons.SkipForward | Buttons.Exit | Buttons.Forward | Buttons.Previous | Buttons.PreviousTrack | Buttons.Next | Buttons.NextTrack | Buttons.Home | Buttons.Reverse | Buttons.PlayPauseToggle | Buttons.Info | Buttons.Subtitle)
        // Need an Input for TV so we just pretend there's a generic input.
        .AddButton("INPUT KODI", "Kodi");

    protected override string GetDisplayName(KodiClient client) => $"{client.DisplayName} - Remote";
}
