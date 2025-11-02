using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;

namespace Neeo.Drivers.PlexApi;

public class PlexMediaPlayerDeviceProvider(
    IHttpClientFactory httpClientFactory,
    IPlexServerDiscovery discovery,
    IPlexTokenStore tokenStore,
    IPlexServerManager serverManager,
    ILogger<PlexMediaPlayerDeviceProvider> logger
) : PlexDeviceProviderBase(
    httpClientFactory,
    discovery,
    tokenStore,
    serverManager,
    logger,
    DeviceType.MediaPlayer,
    "Media Player"
), IPlayerWidgetController
{
    bool IPlayerWidgetController.IsQueueSupported => false;

    string? IPlayerWidgetController.QueueDirectoryLabel => throw new NotSupportedException();

    string? IPlayerWidgetController.RootDirectoryLabel => null;

    public Task SetIsPlayingAsync(string deviceId, bool isPlaying, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    Task IPlayerWidgetController.BrowseQueueDirectoryAsync(string deviceId, DirectoryBuilder builder, CancellationToken cancellationToken) => throw new NotSupportedException();

    Task IPlayerWidgetController.BrowseRootDirectoryAsync(
        string deviceId,
        DirectoryBuilder builder,
        CancellationToken cancellationToken
    ) => this.BrowseRootDirectoryAsync(deviceId, builder, cancellationToken);

    Task<string> IPlayerWidgetController.GetCoverArtAsync(
        string deviceId,
        CancellationToken cancellationToken
    ) => this.GetCoverArtAsync(deviceId, cancellationToken);

    Task<string> IPlayerWidgetController.GetDescriptionAsync(
            string deviceId,
        CancellationToken cancellationToken
    ) => this.GetDescriptionAsync(deviceId, cancellationToken);

    Task<bool> IPlayerWidgetController.GetIsMutedAsync(string deviceId, CancellationToken cancellationToken) => Task.FromResult(false);

    Task<bool> IPlayerWidgetController.GetIsPlayingAsync(
        string deviceId,
        CancellationToken cancellationToken
    ) => Task.FromResult(this.IsPlaying(deviceId));

    Task<bool> IPlayerWidgetController.GetRepeatAsync(string deviceId, CancellationToken cancellationToken) => Task.FromResult(false);

    Task<bool> IPlayerWidgetController.GetShuffleAsync(string deviceId, CancellationToken cancellationToken) => Task.FromResult(false);

    Task<string> IPlayerWidgetController.GetTitleAsync(
        string deviceId,
        CancellationToken cancellationToken
    ) => this.GetTitleAsync(deviceId, cancellationToken);

    Task<double> IPlayerWidgetController.GetVolumeAsync(string deviceId, CancellationToken cancellationToken) => Task.FromResult(0d);

    Task IPlayerWidgetController.HandleQueueDirectoryActionAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken) => throw new NotSupportedException();

    Task IPlayerWidgetController.HandleRootDirectoryActionAsync(
        string deviceId,
        string actionIdentifier,
        CancellationToken cancellationToken
    ) => this.HandleRootDirectoryActionAsync(deviceId, actionIdentifier, cancellationToken);

    Task IPlayerWidgetController.SetIsMutedAsync(string deviceId, bool isMuted, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IPlayerWidgetController.SetRepeatAsync(string deviceId, bool repeat, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IPlayerWidgetController.SetShuffleAsync(string deviceId, bool shuffle, CancellationToken cancellationToken) => Task.CompletedTask;

    Task IPlayerWidgetController.SetVolumeAsync(string deviceId, double volume, CancellationToken cancellationToken) => Task.CompletedTask;

    protected override IDeviceBuilder CreateDevice() => base.CreateDevice()
        .AddPlayerWidget(this);
}
