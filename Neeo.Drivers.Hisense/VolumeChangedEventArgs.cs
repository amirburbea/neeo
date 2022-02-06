using System;

namespace Neeo.Drivers.Hisense;

public sealed class VolumeChangedEventArgs : EventArgs
{
    public VolumeChangedEventArgs(int volume) => this.Volume = volume;

    public int Volume { get; }
}
