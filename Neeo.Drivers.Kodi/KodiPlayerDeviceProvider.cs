using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Lists;

namespace Neeo.Drivers.Kodi;

public sealed class KodiPlayerDeviceProvider(KodiClientManager clientManager, ILogger<KodiRemoteDeviceProvider> logger) : KodiDeviceProviderBase(clientManager, "Player (Kodi)", DeviceType.MediaPlayer, logger), IPlayerWidgetController
{
    public bool IsQueueSupported => false;

    public string? QueueDirectoryLabel { get; }

    public string? RootDirectoryLabel { get; }

    Task<string> IPlayerWidgetController.GetCoverArtAsync(string deviceId) => this.GetCoverArtAsync(deviceId);

    Task<string> IPlayerWidgetController.GetDescriptionAsync(string deviceId) => this.GetDescriptionAsync(deviceId);

    Task<bool> IPlayerWidgetController.GetIsMutedAsync(string deviceId) => this.GetIsMutedAsync(deviceId);

    Task<bool> IPlayerWidgetController.GetIsPlayingAsync(string deviceId) => this.GetIsPlayingAsync(deviceId);

    Task<bool> IPlayerWidgetController.GetRepeatAsync(string deviceId) => Task.FromResult(false);

    Task<bool> IPlayerWidgetController.GetShuffleAsync(string deviceId) => Task.FromResult(false);

    Task<string> IPlayerWidgetController.GetTitleAsync(string deviceId) => this.GetTitleAsync(deviceId);

    Task<double> IPlayerWidgetController.GetVolumeAsync(string deviceId) => this.GetVolumeAsync(deviceId);

    public Task HandleQueueDirectoryActionAsync(string deviceId, string actionIdentifier)
    {
        throw new System.NotImplementedException();
    }

    Task IPlayerWidgetController.HandleRootDirectoryActionAsync(string deviceId, string actionIdentifier) => this.HandleDirectoryActionAsync(deviceId, actionIdentifier);

    Task IPlayerWidgetController.PopulateQueueDirectoryAsync(string deviceId, ListBuilder builder) => this.PopulateQueueDirectoryAsync(deviceId, builder);

    Task IPlayerWidgetController.PopulateRootDirectoryAsync(string deviceId, ListBuilder builder) => this.PopulateRootDirectoryAsync(deviceId, builder);

    public async Task SetIsMutedAsync(string deviceId, bool isMuted)
    {
        if (this.GetClientOrDefault(deviceId) is { } client && KodiDeviceProviderBase.IsClientReady(client) && client.IsMuted != isMuted)
        {
            await client.SendInputCommandAsync(InputCommand.MuteToggle).ConfigureAwait(false);
        }
    }

    public async Task SetIsPlayingAsync(string deviceId, bool value)
    {
        if (this.GetClientOrDefault(deviceId) is { } client && KodiDeviceProviderBase.IsClientReady(client))
        {
            await client.SendInputCommandAsync(value ? InputCommand.Play : InputCommand.Pause).ConfigureAwait(false);
        }
    }

    Task IPlayerWidgetController.SetRepeatAsync(string deviceId, bool repeat) => Task.CompletedTask;

    Task IPlayerWidgetController.SetShuffleAsync(string deviceId, bool shuffle) => Task.CompletedTask;

    Task IPlayerWidgetController.SetVolumeAsync(string deviceId, double volume) => this.SetVolumeAsync(deviceId, volume);

    protected override IDeviceBuilder CreateDevice() => base.CreateDevice()
        .AddPlayerWidget(this)
        .AddButtonGroup(ButtonGroups.Power | ButtonGroups.ChannelZapper | ButtonGroups.ControlPad | ButtonGroups.MenuAndBack)
        .AddButton(Buttons.Stop);

    protected override string GetDisplayName(KodiClient client) => $"{client.DisplayName} - Player";
}
