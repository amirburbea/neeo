using System.Threading.Tasks;
using Neeo.Sdk.Devices.Lists;

namespace Neeo.Sdk.Devices;

public interface IPlayerWidgetCallbacks
{
    Task<string> GetCoverArtUriAsync(string deviceId);

    Task<string> GetDescriptionAsync(string deviceId);

    Task<bool> GetIsMutedAsync(string deviceId);

    Task<bool> GetIsPlayingAsync(string deviceId);

    Task<bool> GetRepeatAsync(string deviceId);

    Task<bool> GetShuffleAsync(string deviceId);

    Task<string> GetTitleAsync(string deviceId);

    Task<double> GetVolumeAsync(string deviceId);

    Task HandleRootDirectoryActionAsync(string deviceId, string actionIdentifier);

    Task PopulateRootDirectoryAsync(string deviceId, IListBuilder builder);

    Task SetIsMutedAsync(string deviceId, bool isMuted);

    Task SetIsPlayingAsync(string deviceId, bool isPlaying);

    Task SetRepeatAsync(string deviceId, bool repeat);

    Task SetShuffleAsync(string deviceId, bool shuffle);

    Task SetVolumeAsync(string deviceId, double volume);
}