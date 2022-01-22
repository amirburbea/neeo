using System.Threading.Tasks;

namespace Neeo.Api.Devices.Controllers;

public interface IFavoritesController : IController
{
    ControllerType IController.Type => ControllerType.Favorites;

    Task<SuccessResult> ExecuteAsync(string deviceId, string favorite);
}

internal sealed class FavoritesController : IFavoritesController
{
    private readonly FavoritesHandler _favoritesHandler;

    public FavoritesController(FavoritesHandler favoritesHandler) => this._favoritesHandler = favoritesHandler;

    public async Task<SuccessResult> ExecuteAsync(string deviceId, string favorite)
    {
        await this._favoritesHandler(deviceId, favorite).ConfigureAwait(false);
        return true;
    }
}