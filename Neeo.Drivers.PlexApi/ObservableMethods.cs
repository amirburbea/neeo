using System;
using System.Reactive.Linq;

namespace Neeo.Drivers.PlexApi;

internal static class ObservableMethods
{
    public static IObservable<T> WhereNotNull<T>(this IObservable<T?> source)
    {
        return source.SelectMany(value => value is not { } item ? Observable.Empty<T>() : Observable.Return(item));
    }
}
