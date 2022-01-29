using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Controllers;

public interface IFavoritesController : IFeature
{
    FeatureType IFeature.Type => FeatureType.Favorites;

    Task<SuccessResponse> ExecuteAsync(string deviceId, string favorite);
}

internal sealed class FavoritesController : IFavoritesController
{
    private readonly FavoritesHandler _favoritesHandler;

    public FavoritesController(FavoritesHandler favoritesHandler) => this._favoritesHandler = favoritesHandler;

    public async Task<SuccessResponse> ExecuteAsync(string deviceId, string favorite)
    {
        await this._favoritesHandler(deviceId, favorite).ConfigureAwait(false);
        return new();
    }
}