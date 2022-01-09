using System;

namespace Remote.Broadlink;

public sealed class DataEventArgs<TData> : EventArgs
{
    public DataEventArgs(TData data) => this.Data = data;

    public TData Data { get; }
}

