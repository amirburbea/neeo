namespace Neeo.Drivers.Kodi.Models;

public readonly record struct PlayerState(
    PlayState PlayState, 
    string NowPlayingLabel, 
    string NowPlayingDescription, 
    string NowPlayingImage
)
{
    internal static readonly PlayerState Defaults = new(PlayState.Stopped, "Nothing is playing now.", string.Empty, Images.Kodi);

    internal static readonly PlayerState Disconnected = new(PlayState.Stopped, "Not connected.", "Ensure Kodi is running.", Images.Kodi);
}

public enum PlayState
{
    Stopped = 0,
    Playing = 1,
    Paused = 2,
}