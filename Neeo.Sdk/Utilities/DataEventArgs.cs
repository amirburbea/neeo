using System;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Generic event for reporting a piece of data.
/// </summary>
/// <typeparam name="TData">Type of data for the event.</typeparam>
public sealed class DataEventArgs<TData> : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="DataEventArgs{TData}"/> with the specified <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The data for the event.</param>
    public DataEventArgs(TData data) => this.Data = data;

    /// <summary>
    /// Gets the data for the event.
    /// </summary>
    public TData Data { get; }

    /// <summary>
    /// Implicitly wraps the <paramref name="data"/> in a <see cref="DataEventArgs{TData}"/>.
    /// </summary>
    /// <param name="data">The data for the event.</param>
    public static implicit operator DataEventArgs<TData>(TData data) => new(data);
}