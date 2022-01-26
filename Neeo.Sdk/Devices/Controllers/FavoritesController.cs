using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Controllers;

public interface IFavoritesController : IController
{
    ControllerType IController.Type => ControllerType.Favorites;

    Task ExecuteAsync(string deviceId, string favorite);
}

internal sealed class FavoritesController : IFavoritesController
{
    private readonly FavoritesHandler _favoritesHandler;

    public FavoritesController(FavoritesHandler favoritesHandler) => this._favoritesHandler = favoritesHandler;

    public Task ExecuteAsync(string deviceId, string favorite) => this._favoritesHandler(deviceId, favorite);
}