using System;

namespace Neeo.Sdk.Utilities;

public sealed class DataEventArgs<TData> : EventArgs
{
    public static implicit operator DataEventArgs<TData>(TData data) => new(data);

    public DataEventArgs(TData data) => this.Data = data;

    public TData Data { get; }
}

