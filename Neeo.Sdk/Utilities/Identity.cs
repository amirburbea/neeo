using System;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Contains a single property <see cref="Identity{T}.Projection"/>, a callback which returns the argument as passed.
/// </summary>
/// <typeparam name="TItem">The type of the callback argument.</typeparam>
public static class Identity<TItem>
{
    /// <summary>
    /// The identity projection.
    /// </summary>
    public static readonly Func<TItem, TItem> Projection = static item => item;
}