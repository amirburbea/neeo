using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Directories;

namespace Neeo.Sdk.Devices.Features;

/// <summary>
/// Feature support for browsable directories.
/// </summary>
public interface IDirectoryFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Directory;

    /// <summary>
    /// Given a set of <paramref name="parameters"/>, browse the directory and populate a list with
    /// its contents.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="parameters">
    /// The parameters relating to the directory to browse and an offset and limit if applicable.
    /// </param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<DirectoryData> BrowseAsync(string deviceId, BrowseParameters parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handle a request by a user to perform an action in a directory such as opening a file.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="actionIdentifier">
    /// The identifier for the action to be performed (such as the file to be opened).
    /// </param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<SuccessResponse> PerformActionAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken = default);
}

internal sealed class DirectoryFeature(DirectoryBrowser browser, DirectoryActionHandler actionHandler, string browseIdentifier) : IDirectoryFeature
{
    public async Task<DirectoryData> BrowseAsync(string deviceId, BrowseParameters parameters, CancellationToken cancellationToken)
    {
        DirectoryBuilder builder = new(
            parameters.BrowseIdentifier is { Length: > 0 }
              ? parameters
              : parameters with { BrowseIdentifier = browseIdentifier } // Override.
        );
        await browser(deviceId, builder, cancellationToken).ConfigureAwait(false);
        return builder.Build();
    }

    public async Task<SuccessResponse> PerformActionAsync(string deviceId, string actionIdentifier, CancellationToken cancellationToken)
    {
        await actionHandler(deviceId, actionIdentifier, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
