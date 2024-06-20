using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Lists;

namespace Neeo.Drivers.Kodi;

public sealed class KodiPlayerDeviceProvider(KodiClientManager clientManager, ILogger<KodiPlayerDeviceProvider> logger) : KodiDeviceProviderBase(clientManager, "Player", DeviceType.MediaPlayer, logger), IPlayerWidgetController
{
    public bool IsQueueSupported => false;

    public string? QueueDirectoryLabel { get; }

    public string? RootDirectoryLabel { get; }

    Task<string> IPlayerWidgetController.GetCoverArtAsync(string deviceId, CancellationToken cancellationToken) => this.GetCoverArt(deviceId);

    Task<string> IPlayerWidgetController.GetDescriptionAsync(string deviceId, CancellationToken cancellationToken) => this.GetDescription(deviceId);

    Task<bool> IPlayerWidgetController.GetIsMutedAsync(string deviceId, CancellationToken cancellationToken) => this.GetIsMutedAsync(deviceId);

    Task<bool> IPlayerWidgetController.GetIsPlayingAsync(string deviceId, CancellationToken cancellationToken) => this.GetIsPlaying(deviceId);

    Task<bool> IPlayerWidgetController.GetRepeatAsync(string deviceId, CancellationToken cancellationToken) => Task.FromResult(false);

    Task<bool> IPlayerWidgetController.GetShuffleAsync(string deviceId, CancellationToken cancellationToken) => Task.FromResult(false);

    Task<string> IPlayerWidgetController.GetTitleAsync(string deviceId, CancellationToken cancellationToken) => this.GetTitle(deviceId);

    Task<double> IPlayerWidgetController.GetVolumeAsync(string deviceId, CancellationToken cancellationToken) => this.GetVolume(deviceId);

    public Task HandleQueueDirectoryActionAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }

    Task IPlayerWidgetController.HandleRootDirectoryActionAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken) => this.HandleDirectoryActionAsync(deviceId, actionIdentifier, cancellationToken);

    Task IPlayerWidgetController.PopulateQueueDirectoryAsync(string deviceId, DirectoryBuilder builder, CancellationToken cancellationToken) => this.PopulateQueueDirectoryAsync(deviceId, builder, cancellationToken);

    Task IPlayerWidgetController.PopulateRootDirectoryAsync(string deviceId, DirectoryBuilder builder, CancellationToken cancellationToken) => this.PopulateRootDirectoryAsync(deviceId, builder, cancellationToken);

    public async Task SetIsMutedAsync(string deviceId, bool isMuted, CancellationToken cancellationToken)
    {
        if (this.GetClientOrDefault(deviceId) is { } client && KodiDeviceProviderBase.IsClientReady(client) && client.IsMuted != isMuted)
        {
            await client.SendInputCommandAsync(InputCommand.MuteToggle, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task SetIsPlayingAsync(string deviceId, bool value, CancellationToken cancellationToken)
    {
        if (this.GetClientOrDefault(deviceId) is { } client && KodiDeviceProviderBase.IsClientReady(client))
        {
            await client.SendInputCommandAsync(value ? InputCommand.Play : InputCommand.Pause, cancellationToken).ConfigureAwait(false);
        }
    }

    Task IPlayerWidgetController.SetRepeatAsync(string deviceId, bool repeat, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IPlayerWidgetController.SetShuffleAsync(string deviceId, bool shuffle, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IPlayerWidgetController.SetVolumeAsync(string deviceId, double volume, CancellationToken cancellationToken) => this.SetVolumeAsync(deviceId, volume, cancellationToken);

    protected override IDeviceBuilder CreateDevice() => base.CreateDevice()
        .AddPlayerWidget(this)
        .AddButtonGroup(ButtonGroups.Power | ButtonGroups.ChannelZapper | ButtonGroups.ControlPad | ButtonGroups.MenuAndBack)
        .AddButton(Buttons.Stop);

    protected override string GetDisplayName(KodiClient client) => $"{client.DisplayName} - Player";
}
