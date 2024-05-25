using System;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Generic event arguments for reporting data.
/// </summary>
/// <typeparam name="TData">Type of data for the event.</typeparam>
/// <remarks>
/// Initializes a new instance of <see cref="DataEventArgs{TData}"/> with the specified <paramref name="data"/>.
/// </remarks>
/// <param name="data">The data for the event.</param>
public sealed class DataEventArgs<TData>(TData data) : EventArgs
{
    /// <summary>
    /// Gets the data for the event.
    /// </summary>
    public TData Data { get; } = data;

    /// <summary>
    /// Implicitly wraps the <paramref name="data"/> in a <see cref="DataEventArgs{TData}"/>.
    /// </summary>
    /// <param name="data">The data for the event.</param>
    public static implicit operator DataEventArgs<TData>(TData data) => new(data);
}
