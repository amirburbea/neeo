using System;

namespace Neeo.Drivers.Hisense;

public sealed class StateChangedEventArgs : EventArgs
{
    public StateChangedEventArgs(IState state) => this.State = state;

    public IState State { get; }
}
