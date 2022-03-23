namespace Neeo.Drivers.Hisense;

public record struct AppState(AppInfo App) : IState
{
    StateType IState.Type => StateType.App;
}