using System;
using System.Diagnostics.CodeAnalysis;

namespace Neeo.Drivers.Hisense;

public readonly struct AppState : IState, IEquatable<AppState>
{
    public AppState(AppInfo app) => this.App = app;

    public AppInfo App { get; }

    StateType IState.Type => StateType.App;

    public static bool operator !=(AppState left, AppState right) => !left.Equals(right);

    public static bool operator ==(AppState left, AppState right) => left.Equals(right);

    public override bool Equals([NotNullWhen(true)] object? obj) => obj is AppState state && this.Equals(state);

    public bool Equals(AppState other) => this.App == other.App;

    public override int GetHashCode() => this.App.GetHashCode();
}