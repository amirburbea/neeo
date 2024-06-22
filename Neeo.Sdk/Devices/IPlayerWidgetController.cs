using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Directories;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Controller <see langword="interface"/> for devices to display the player widget.
/// </summary>
public interface IPlayerWidgetController
{
    /// <summary>
    /// Gets a value indicating if the device supports a playlist queue.
    /// </summary>
    bool IsQueueSupported { get; }

    /// <summary>
    /// If supported (via <see cref="IsQueueSupported"/>), gets a label to display for the queue directory.
    /// Defaulted to "Queue" if <see langword="null" />.
    /// </summary>
    string? QueueDirectoryLabel { get; }

    /// <summary>
    /// Gets a label to display for the root directory.
    /// Defaulted to "Root" if <see langword="null" />.
    /// </summary>
    string? RootDirectoryLabel { get; }

    /// <summary>
    /// If supported (via <see cref="IsQueueSupported"/>), asynchronously handles a request by the NEEO Brain to browse the queue directory.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="builder">Directory builder which can be used to populate the directory entries.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task BrowseQueueDirectoryAsync(string deviceId, DirectoryBuilder builder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously handles a request by the NEEO Brain to browse the root directory.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="builder">Directory builder which can be used to populate the directory entries.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task BrowseRootDirectoryAsync(string deviceId, DirectoryBuilder builder, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets the URL of the cover art for the currently playing item.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<string> GetCoverArtAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets the description of the currently playing item.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<string> GetDescriptionAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets a value indicating if the device is currently muted.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<bool> GetIsMutedAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets a value indicating if the device is currently playing.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<bool> GetIsPlayingAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets a value indicating if the device is currently set to repeat play.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<bool> GetRepeatAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets a value indicating if the device is currently set to shuffle play.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<bool> GetShuffleAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets the title of the currently playing item.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<string> GetTitleAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously gets the current device volume.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<double> GetVolumeAsync(string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously handles an action from the queue directory.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="actionIdentifier">The identifier for the action</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task HandleQueueDirectoryActionAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously handles an action from the root directory.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="actionIdentifier">The identifier for the action</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task HandleRootDirectoryActionAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sets if the device should be muted.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="isMuted">The value to set.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task SetIsMutedAsync(string deviceId, bool isMuted, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sets if the device should be playing.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="isPlaying">The value to set.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task SetIsPlayingAsync(string deviceId, bool isPlaying, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sets if the device should be set to repeat play.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="repeat">The value to set.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task SetRepeatAsync(string deviceId, bool repeat, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sets if the device should be set to shuffle play.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="shuffle">The value to set.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task SetShuffleAsync(string deviceId, bool shuffle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously sets the current device volume.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="volume">The volume value to set, between 0 and 100.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task SetVolumeAsync(string deviceId, double volume, CancellationToken cancellationToken = default);
}
