using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Neeo.Sdk.Devices.Features;

/// <summary>
/// Feature support for custom favorites.
/// </summary>
public interface IFavoritesFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Favorites;

    /// <summary>
    /// Executes a favorite for a device.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="favorite">The favorite to execute.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<SuccessResponse> ExecuteAsync(string deviceId, string favorite);
}

internal sealed class FavoritesFeature : IFavoritesFeature
{
    private readonly FavoriteHandler _handler;

    public FavoritesFeature(FavoriteHandler handler) => this._handler = handler ?? throw new ArgumentNullException(nameof(handler));

    public async Task<SuccessResponse> ExecuteAsync(string deviceId, string favorite)
    {
        await this._handler(deviceId, favorite).ConfigureAwait(false);
        return true;
    }
}