using System.Threading.Tasks;
using Neeo.Sdk.Devices.Lists;

namespace Neeo.Sdk.Devices;

public interface IPlayerWidgetController
{
    bool IsQueueSupported { get; }

    string? QueueDirectoryLabel { get; }

    string? RootDirectoryLabel { get; }

    Task<string> GetCoverArtAsync(string deviceId);

    Task<string> GetDescriptionAsync(string deviceId);

    Task<bool> GetIsMutedAsync(string deviceId);

    Task<bool> GetIsPlayingAsync(string deviceId);

    Task<bool> GetRepeatAsync(string deviceId);

    Task<bool> GetShuffleAsync(string deviceId);

    Task<string> GetTitleAsync(string deviceId);

    Task<double> GetVolumeAsync(string deviceId);

    Task HandleQueueDirectoryActionAsync(string deviceId, string actionIdentifier);

    Task HandleRootDirectoryActionAsync(string deviceId, string actionIdentifier);

    Task PopulateQueueDirectoryAsync(string deviceId, ListBuilder builder);

    Task PopulateRootDirectoryAsync(string deviceId, ListBuilder builder);

    Task SetIsMutedAsync(string deviceId, bool isMuted);

    Task SetIsPlayingAsync(string deviceId, bool isPlaying);

    Task SetRepeatAsync(string deviceId, bool repeat);

    Task SetShuffleAsync(string deviceId, bool shuffle);

    Task SetVolumeAsync(string deviceId, double volume);
}