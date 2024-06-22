using System;
using System.Threading;
using System.Threading.Tasks;

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
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task<SuccessResponse> ExecuteAsync(string deviceId, string favorite, CancellationToken cancellationToken = default);
}

internal sealed class FavoritesFeature(FavoriteHandler handler) : IFavoritesFeature
{
    private readonly FavoriteHandler _handler = handler ?? throw new ArgumentNullException(nameof(handler));

    public async Task<SuccessResponse> ExecuteAsync(string deviceId, string favorite, CancellationToken cancellationToken)
    {
        await this._handler(deviceId, favorite, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
