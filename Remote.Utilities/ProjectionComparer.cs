using System;
using System.Collections.Generic;

namespace Remote.Utilities;

public static class ProjectionComparer
{
    public static ProjectionComparer<T, TKey> Create<T, TKey>(Func<T, TKey> projection, IComparer<TKey>? keyComparer = default) => new(projection, keyComparer);
}

public sealed class ProjectionComparer<T, TKey> : Comparer<T>
{
    public ProjectionComparer(Func<T, TKey> projection, IComparer<TKey>? keyComparer = default)
    {
        this.Projection = projection;
        this.KeyComparer = keyComparer ?? Comparer<TKey>.Default;
    }

    public IComparer<TKey> KeyComparer { get; }

    public Func<T, TKey> Projection { get; }

    public override int Compare(T? x, T? y)
    {
        if (x is null)
        {
            return y is null ? 0 : -1;
        }
        return y is null ? 1 : this.KeyComparer.Compare(this.Projection(x), this.Projection(y));
    }
}