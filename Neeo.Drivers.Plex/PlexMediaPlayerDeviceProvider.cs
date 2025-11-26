using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neeo.Sdk;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Directories;

namespace Neeo.Drivers.Plex;

public sealed class PlexMediaPlayerDeviceProvider(
    IHttpClientFactory httpClientFactory,
    IPlexServerManager serverManager,
    IPlexTokenStore tokenStore,
    [FromKeyedServices(Startup.Task)] Task startupTask,
    ILogger<PlexMediaPlayerDeviceProvider> logger
) : PlexDeviceProviderBase(
    httpClientFactory,
    serverManager, 
    tokenStore, 
    startupTask, 
    logger, 
    DeviceType.MediaPlayer, 
    "Media Player"
), IPlayerWidgetController
{
    bool IPlayerWidgetController.IsQueueSupported => false;

    string? IPlayerWidgetController.QueueDirectoryLabel => throw new NotSupportedException();

    string? IPlayerWidgetController.RootDirectoryLabel => null;

    Task IPlayerWidgetController.BrowseQueueDirectoryAsync(string machineIdentifier, IDirectoryBuilder builder, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task IPlayerWidgetController.BrowseRootDirectoryAsync(string machineIdentifier, IDirectoryBuilder builder, CancellationToken cancellationToken)
    {
        return this.BrowseDirectoryAsync(machineIdentifier, builder, cancellationToken);
    }

    Task<string> IPlayerWidgetController.GetCoverArtAsync(string machineIdentifier, CancellationToken cancellationToken)
    {
        return Task.FromResult(this.GetCoverArt(machineIdentifier));
    }

    Task<string> IPlayerWidgetController.GetDescriptionAsync(string machineIdentifier, CancellationToken cancellationToken)
    {
        return Task.FromResult(this.GetDescription(machineIdentifier));
    }

    Task<bool> IPlayerWidgetController.GetIsMutedAsync(string machineIdentifier, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    Task<bool> IPlayerWidgetController.GetIsPlayingAsync(string machineIdentifier, CancellationToken cancellationToken)
    {
        return Task.FromResult(this.IsPlaying(machineIdentifier));
    }

    Task<bool> IPlayerWidgetController.GetRepeatAsync(string machineIdentifier, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    Task<bool> IPlayerWidgetController.GetShuffleAsync(string machineIdentifier, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    Task<string> IPlayerWidgetController.GetTitleAsync(string machineIdentifier, CancellationToken cancellationToken)
    {
        return Task.FromResult(this.GetTitle(machineIdentifier));
    }

    Task<double> IPlayerWidgetController.GetVolumeAsync(string machineIdentifier, CancellationToken cancellationToken)
    {
        return Task.FromResult(0d);
    }

    Task IPlayerWidgetController.HandleQueueDirectoryActionAsync(string machineIdentifier, string actionIdentifier, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    Task IPlayerWidgetController.HandleRootDirectoryActionAsync(string machineIdentifier, string actionIdentifier, CancellationToken cancellationToken)
    {
        return this.HandleDirectoryActionAsync(machineIdentifier, actionIdentifier, cancellationToken);
    }

    Task IPlayerWidgetController.SetIsMutedAsync(string machineIdentifier, bool isMuted, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    Task IPlayerWidgetController.SetIsPlayingAsync(string machineIdentifier, bool value, CancellationToken cancellationToken)
    {
        return this.GetServer(machineIdentifier) is { } server
            ? server.SendPlaybackCommandAsync(value ? PlaybackCommand.Play : PlaybackCommand.Pause, cancellationToken)
            : Task.CompletedTask;
    }

    Task IPlayerWidgetController.SetRepeatAsync(string machineIdentifier, bool repeat, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    Task IPlayerWidgetController.SetShuffleAsync(string machineIdentifier, bool shuffle, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    Task IPlayerWidgetController.SetVolumeAsync(string machineIdentifier, double volume, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override IDeviceBuilder CreateDevice() => base.CreateDevice()
        .AddPlayerWidget(this)
        .AddButton(Buttons.Stop)
        .SetDriverVersion(12);
}
