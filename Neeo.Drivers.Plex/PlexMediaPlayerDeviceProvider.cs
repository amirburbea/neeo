using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;

namespace Neeo.Drivers.Plex;

public sealed class PlexMediaPlayerDeviceProvider(
    IHttpClientFactory httpClientFactory,
    IPlexServerDiscovery serverDiscovery,
    IPlexServerManager serverManager,
    IPlexTokenStore tokenStore,
    ILogger<PlexMediaPlayerDeviceProvider> logger
) : PlexDeviceProviderBase(httpClientFactory, serverDiscovery, serverManager, tokenStore, logger, DeviceType.MediaPlayer, "Media Player"), IPlayerWidgetController
{
    bool IPlayerWidgetController.IsQueueSupported => false;

    string? IPlayerWidgetController.QueueDirectoryLabel => throw new NotSupportedException();

    string? IPlayerWidgetController.RootDirectoryLabel => null;

    Task IPlayerWidgetController.BrowseQueueDirectoryAsync(
        string machineIdentifier,
        IDirectoryBuilder builder,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();

    Task IPlayerWidgetController.BrowseRootDirectoryAsync(
        string machineIdentifier,
        IDirectoryBuilder builder,
        CancellationToken cancellationToken
    ) => this.BrowseDirectoryAsync(machineIdentifier, builder, cancellationToken);

    Task<string> IPlayerWidgetController.GetCoverArtAsync(
        string machineIdentifier,
        CancellationToken cancellationToken
    ) => Task.FromResult(this.GetCoverArt(machineIdentifier));

    Task<string> IPlayerWidgetController.GetDescriptionAsync(
        string machineIdentifier,
        CancellationToken cancellationToken
    ) => Task.FromResult(this.GetDescription(machineIdentifier));

    Task<bool> IPlayerWidgetController.GetIsMutedAsync(
        string machineIdentifier,
        CancellationToken cancellationToken
    ) => Task.FromResult(false);

    Task<bool> IPlayerWidgetController.GetIsPlayingAsync(
        string machineIdentifier,
        CancellationToken cancellationToken
    ) => Task.FromResult(this.IsPlaying(machineIdentifier));

    Task<bool> IPlayerWidgetController.GetRepeatAsync(
        string machineIdentifier,
        CancellationToken cancellationToken
    ) => Task.FromResult(false);

    Task<bool> IPlayerWidgetController.GetShuffleAsync(
        string machineIdentifier,
        CancellationToken cancellationToken
    ) => Task.FromResult(false);

    Task<string> IPlayerWidgetController.GetTitleAsync(
        string machineIdentifier,
        CancellationToken cancellationToken
    ) => Task.FromResult(this.GetTitle(machineIdentifier));

    Task<double> IPlayerWidgetController.GetVolumeAsync(
        string machineIdentifier,
        CancellationToken cancellationToken
    ) => Task.FromResult(0d);

    Task IPlayerWidgetController.HandleQueueDirectoryActionAsync(
        string machineIdentifier,
        string actionIdentifier,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();

    Task IPlayerWidgetController.HandleRootDirectoryActionAsync(
        string machineIdentifier,
        string actionIdentifier,
        CancellationToken cancellationToken
    ) => this.HandleDirectoryActionAsync(machineIdentifier, actionIdentifier, cancellationToken);

    Task IPlayerWidgetController.SetIsMutedAsync(
        string machineIdentifier,
        bool isMuted,
        CancellationToken cancellationToken
    ) => Task.CompletedTask;

    Task IPlayerWidgetController.SetIsPlayingAsync(
        string machineIdentifier,
        bool value,
        CancellationToken cancellationToken
    ) => this.GetServer(machineIdentifier) switch
    {
        { } server => server.SendPlaybackCommandAsync(value ? PlaybackCommand.Play : PlaybackCommand.Pause, cancellationToken),
        _ => Task.CompletedTask
    };

    Task IPlayerWidgetController.SetRepeatAsync(
        string machineIdentifier,
        bool repeat,
        CancellationToken cancellationToken
    ) => Task.CompletedTask;

    Task IPlayerWidgetController.SetShuffleAsync(
        string machineIdentifier,
        bool shuffle,
        CancellationToken cancellationToken
    ) => Task.CompletedTask;

    Task IPlayerWidgetController.SetVolumeAsync(
        string machineIdentifier,
        double volume,
        CancellationToken cancellationToken
    ) => Task.CompletedTask;

    protected override IDeviceBuilder CreateDevice() => base.CreateDevice()
        .AddPlayerWidget(this)
        .AddButton(Buttons.Stop);
}
