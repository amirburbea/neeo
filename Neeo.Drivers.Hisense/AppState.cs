namespace Neeo.Drivers.Hisense;

public readonly record struct AppState(AppInfo App) : IState
{
    StateType IState.Type => StateType.App;
}