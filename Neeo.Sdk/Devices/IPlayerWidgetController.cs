using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Lists;

namespace Neeo.Sdk.Devices;

public interface IPlayerWidgetController
{
    bool IsQueueSupported { get; }

    string? QueueDirectoryLabel { get; }

    string? RootDirectoryLabel { get; }

    Task<string> GetCoverArtAsync(string deviceId, CancellationToken cancellationToken = default);

    Task<string> GetDescriptionAsync(string deviceId, CancellationToken cancellationToken = default);

    Task<bool> GetIsMutedAsync(string deviceId, CancellationToken cancellationToken = default);

    Task<bool> GetIsPlayingAsync(string deviceId, CancellationToken cancellationToken = default);

    Task<bool> GetRepeatAsync(string deviceId, CancellationToken cancellationToken = default);

    Task<bool> GetShuffleAsync(string deviceId, CancellationToken cancellationToken = default);

    Task<string> GetTitleAsync(string deviceId, CancellationToken cancellationToken = default);

    Task<double> GetVolumeAsync(string deviceId, CancellationToken cancellationToken = default);

    Task HandleQueueDirectoryActionAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken = default);

    Task HandleRootDirectoryActionAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken = default);

    Task PopulateQueueDirectoryAsync(string deviceId, ListBuilder builder, CancellationToken cancellationToken = default);

    Task PopulateRootDirectoryAsync(string deviceId, ListBuilder builder, CancellationToken cancellationToken = default);

    Task SetIsMutedAsync(string deviceId, bool isMuted, CancellationToken cancellationToken = default);

    Task SetIsPlayingAsync(string deviceId, bool isPlaying, CancellationToken cancellationToken = default);

    Task SetRepeatAsync(string deviceId, bool repeat, CancellationToken cancellationToken = default);

    Task SetShuffleAsync(string deviceId, bool shuffle, CancellationToken cancellationToken = default);

    Task SetVolumeAsync(string deviceId, double volume, CancellationToken cancellationToken = default);
}
