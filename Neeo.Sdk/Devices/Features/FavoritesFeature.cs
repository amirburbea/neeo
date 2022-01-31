using System;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Features;

public interface IFavoritesFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Favorites;

    Task<SuccessResponse> ExecuteAsync(string deviceId, string favorite);
}

internal sealed class FavoritesFeature : IFavoritesFeature
{
    private readonly FavoritesHandler _favoritesHandler;

    public FavoritesFeature(FavoritesHandler favoritesHandler) => this._favoritesHandler = favoritesHandler??throw new ArgumentNullException(nameof(favoritesHandler));

    public async Task<SuccessResponse> ExecuteAsync(string deviceId, string favorite)
    {
        await this._favoritesHandler(deviceId, favorite).ConfigureAwait(false);
        return true;
    }
}