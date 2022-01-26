using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Neeo.Sdk.Utilities;

internal static class ProjectionCollection
{
    public static ProjectionCollection<T, TValue> Create<T, TValue>(IReadOnlyCollection<T> collection, Func<T, TValue> projection) => new(collection, projection);
}

internal sealed class ProjectionCollection<T, TValue> : IReadOnlyCollection<TValue>
{
    private readonly IReadOnlyCollection<T> _collection;
    private readonly Func<T, TValue> _projection;

    public ProjectionCollection(IReadOnlyCollection<T> collection, Func<T, TValue> projection)
    {
        (this._collection, this._projection) = (collection ?? throw new ArgumentNullException(nameof(collection)), projection ?? throw new ArgumentNullException(nameof(projection)));
    }

    public int Count => this._collection.Count;

    public IEnumerator<TValue> GetEnumerator() => this._collection.Select(this._projection).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}