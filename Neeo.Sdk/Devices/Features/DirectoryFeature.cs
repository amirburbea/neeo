using System;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Lists;

namespace Neeo.Sdk.Devices.Features;

public interface IDirectoryFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Directory;

    Task<IListBuilder> BrowseAsync(string deviceId, BrowseParameters parameters);

    Task PerformActionAsync(string deviceId, string actionIdentifier);
}

internal sealed class DirectoryFeature : IDirectoryFeature
{
    private readonly DirectoryActionHandler _actionHandler;
    private readonly DeviceDirectoryPopulator _populator;

    public DirectoryFeature(DeviceDirectoryPopulator populator, DirectoryActionHandler actionHandler)
    {
        this._populator = populator ?? throw new ArgumentNullException(nameof(populator));
        this._actionHandler = actionHandler ?? throw new ArgumentNullException(nameof(actionHandler));
    }

    public async Task<IListBuilder> BrowseAsync(string deviceId, BrowseParameters parameters)
    {
        ListBuilder builder = new(parameters);
        await this._populator(deviceId, builder).ConfigureAwait(false);
        return builder;
    }

    public Task PerformActionAsync(string deviceId, string actionIdentifier) => this._actionHandler(deviceId, actionIdentifier);
}