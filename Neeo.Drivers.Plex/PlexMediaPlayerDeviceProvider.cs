using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;

namespace Neeo.Drivers.Plex;

public sealed class PlexMediaPlayerDeviceProvider(
    IHttpClientFactory httpClientFactory,
    IPlexServerDiscovery discovery,
    IPlexTokenStore tokenStore,
    IPlexServerManager serverManager,
    Task<ISdkEnvironment> sdkStartupTask,
    ILogger<PlexMediaPlayerDeviceProvider> logger
) : PlexDeviceProviderBase(httpClientFactory, discovery, tokenStore, serverManager, sdkStartupTask, logger, DeviceType.MediaPlayer, "Media Player"), IPlayerWidgetController
{
    bool IPlayerWidgetController.IsQueueSupported => false;

    string? IPlayerWidgetController.QueueDirectoryLabel => throw new NotSupportedException();

    string? IPlayerWidgetController.RootDirectoryLabel => null;

    Task IPlayerWidgetController.BrowseQueueDirectoryAsync(string serverName, DirectoryBuilder builder, CancellationToken cancellationToken) => throw new NotSupportedException();

    Task IPlayerWidgetController.BrowseRootDirectoryAsync(string serverName, DirectoryBuilder builder, CancellationToken cancellationToken)
    {
        return this.BrowseDirectoryAsync(serverName, builder, cancellationToken);
    }

    Task<string> IPlayerWidgetController.GetCoverArtAsync(string serverName, CancellationToken cancellationToken) => Task.FromResult(this.GetCoverArt(serverName));

    Task<string> IPlayerWidgetController.GetDescriptionAsync(string serverName, CancellationToken cancellationToken) => Task.FromResult(this.GetDescription(serverName));

    Task<bool> IPlayerWidgetController.GetIsMutedAsync(string serverName, CancellationToken cancellationToken) => Task.FromResult(false);

    Task<bool> IPlayerWidgetController.GetIsPlayingAsync(string serverName, CancellationToken cancellationToken) => Task.FromResult(this.IsPlaying(serverName));

    Task<bool> IPlayerWidgetController.GetRepeatAsync(string serverName, CancellationToken cancellationToken) => Task.FromResult(false);

    Task<bool> IPlayerWidgetController.GetShuffleAsync(string serverName, CancellationToken cancellationToken) => Task.FromResult(false);

    Task<string> IPlayerWidgetController.GetTitleAsync(string serverName, CancellationToken cancellationToken) => Task.FromResult(this.GetTitle(serverName));

    Task<double> IPlayerWidgetController.GetVolumeAsync(string serverName, CancellationToken cancellationToken) => Task.FromResult(0d);

    Task IPlayerWidgetController.HandleQueueDirectoryActionAsync(string serverName, string actionIdentifier, CancellationToken cancellationToken) => throw new NotSupportedException();

    Task IPlayerWidgetController.HandleRootDirectoryActionAsync(string serverName, string actionIdentifier, CancellationToken cancellationToken)
    {
        return this.HandleDirectoryActionAsync(serverName, actionIdentifier, cancellationToken);
    }

    Task IPlayerWidgetController.SetIsMutedAsync(string serverName, bool isMuted, CancellationToken cancellationToken) => Task.CompletedTask;

    async Task IPlayerWidgetController.SetIsPlayingAsync(string serverName, bool value, CancellationToken cancellationToken)
    {
        if (this.GetServer(serverName) is { } server)
        {
            await server.SendPlaybackCommandAsync(value ? PlaybackCommand.Play : PlaybackCommand.Pause, cancellationToken).ConfigureAwait(false);
        }
    }

    Task IPlayerWidgetController.SetRepeatAsync(string serverName, bool repeat, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IPlayerWidgetController.SetShuffleAsync(string serverName, bool shuffle, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IPlayerWidgetController.SetVolumeAsync(string serverName, double volume, CancellationToken cancellationToken) => Task.CompletedTask;

    protected override IDeviceBuilder CreateDevice() => base.CreateDevice()
        .AddPlayerWidget(this)
        .AddButton(Buttons.Stop);
}
