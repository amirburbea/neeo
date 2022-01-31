﻿using System.Threading.Tasks;
using Neeo.Sdk.Devices.Lists;

namespace Neeo.Sdk.Devices.Features;

public interface IDirectoryFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Directory;

    Task<IListBuilder> BrowseAsync(string deviceId, ListParameters parameters);

    Task<SuccessResponse> PerformActionAsync(string deviceId, string actionIdentifier);
}

internal sealed class DirectoryFeature : IDirectoryFeature
{
    private readonly DirectoryActionHandler _actionHandler;
    private readonly DeviceDirectoryPopulator _populator;

    public DirectoryFeature(DeviceDirectoryPopulator populator, DirectoryActionHandler actionHandler)
    {
        (this._populator, this._actionHandler) = (populator, actionHandler);
    }

    public async Task<IListBuilder> BrowseAsync(string deviceId, ListParameters parameters)
    {
        ListBuilder builder = new(parameters);
        await this._populator(deviceId, builder).ConfigureAwait(false);
        return builder;
    }

    public async Task<SuccessResponse> PerformActionAsync(string deviceId, string actionIdentifier)
    {
        await this._actionHandler(deviceId, actionIdentifier).ConfigureAwait(false);
        return new();
    }
}