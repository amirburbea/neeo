using System.Threading.Tasks;
using Neeo.Sdk.Devices.Lists;

namespace Neeo.Sdk.Devices.Features;

public interface IDirectoryFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Directory;

    Task<IListBuilder> BrowseAsync(string deviceId, BrowseParameters parameters);

    Task<SuccessResponse> PerformActionAsync(string deviceId, string actionIdentifier);
}

internal sealed class DirectoryFeature : IDirectoryFeature
{
    private readonly DirectoryActionHandler _actionHandler;
    private readonly DeviceDirectoryBrowser _browser;

    public DirectoryFeature(DeviceDirectoryBrowser browser, DirectoryActionHandler actionHandler)
    {
        (this._browser, this._actionHandler) = (browser, actionHandler);
    }

    public Task<IListBuilder> BrowseAsync(string deviceId, BrowseParameters parameters) => this._browser(deviceId, parameters);

    public async Task<SuccessResponse> PerformActionAsync(string deviceId, string actionIdentifier)
    {
        await this._actionHandler(deviceId, actionIdentifier).ConfigureAwait(false);
        return new();
    }
}