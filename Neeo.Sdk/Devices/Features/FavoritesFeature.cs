using System;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Features;

public interface IFavoritesFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Favorites;

    Task ExecuteAsync(string deviceId, string favorite);
}

internal sealed class FavoritesFeature : IFavoritesFeature
{
    private readonly FavoriteHandler _handler;

    public FavoritesFeature(FavoriteHandler handler) => this._handler = handler ?? throw new ArgumentNullException(nameof(handler));

    public Task ExecuteAsync(string deviceId, string favorite) => this._handler(deviceId, favorite);
}