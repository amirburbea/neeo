using System;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Lists;

namespace Neeo.Sdk.Devices.Features;

/// <summary>
/// Feature support for browsable directories.
/// </summary>
public interface IDirectoryFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Directory;

    /// <summary>
    /// Given a set of <paramref name="parameters" />, browse the directory and populate a list with its contents.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="parameters">The parameters relating to the directory to browse and an offset and limit if applicable.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<IListBuilder> BrowseAsync(string deviceId, BrowseParameters parameters);

    /// <summary>
    /// Handle a request by a user to perform an action in a directory such as opening a file.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="actionIdentifier">The identifier for the action to be performed (such as the file to be opened).</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task PerformActionAsync(string deviceId, string actionIdentifier);
}

internal sealed class DirectoryFeature : IDirectoryFeature
{
    private readonly DirectoryActionHandler _actionHandler;
    private readonly DirectoryBrowser _browser;
    private readonly string? _identifier;

    public DirectoryFeature(DirectoryBrowser browser, DirectoryActionHandler actionHandler, string? identifier = default)
    {
        this._browser = browser ?? throw new ArgumentNullException(nameof(browser));
        this._actionHandler = actionHandler ?? throw new ArgumentNullException(nameof(actionHandler));
        this._identifier = identifier;
    }

    public async Task<IListBuilder> BrowseAsync(string deviceId, BrowseParameters parameters)
    {
        ListBuilder builder = new(string.IsNullOrEmpty(parameters.BrowseIdentifier) && !string.IsNullOrEmpty(this._identifier) ? parameters with { BrowseIdentifier = this._identifier } : parameters);
        await this._browser(deviceId, builder).ConfigureAwait(false);
        return builder;
    }

    public Task PerformActionAsync(string deviceId, string actionIdentifier) => this._actionHandler(deviceId, actionIdentifier);
}