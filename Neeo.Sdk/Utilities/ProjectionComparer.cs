using System;
using System.Collections.Generic;

namespace Neeo.Sdk.Utilities;

/// <summary>
///
/// </summary>
public static class ProjectionComparer
{
    /// <summary>
    ///
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="projection"></param>
    /// <param name="comparer"></param>
    /// <returns></returns>
    public static ProjectionComparer<T, TResult> Create<T, TResult>(
        Func<T, TResult> projection,
        IComparer<TResult>? comparer = null
    ) => new(projection, comparer);
}

/// <summary>
///
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TResult"></typeparam>
/// <param name="projection"></param>
/// <param name="comparer">
/// Optional - the comparer to use to compare the <paramref name="projection"/> results,
/// defaulted to <see cref="EqualityComparer{T}.Default"/> when left <see langword="null"/>.
/// </param>
public sealed class ProjectionComparer<T, TResult>(Func<T, TResult> projection, IComparer<TResult>? comparer = null) : Comparer<T>
{
    private readonly IComparer<TResult> _comparer = comparer ?? Comparer<TResult>.Default;
    private readonly Func<T, TResult> _projection = projection ?? throw new ArgumentNullException(nameof(projection));

    /// <summary>
    /// Compares two objects and returns a value indicating whether one is less than,
    /// equal to, or greater than the other.
    /// </summary>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    public override int Compare(T? x, T? y) => (x, y) switch
    {
        (null, null) => 0,
        (null, not null) => -1,
        (not null, null) => 1,
        _ => this._comparer.Compare(this._projection(x), this._projection(y))
    };
}
