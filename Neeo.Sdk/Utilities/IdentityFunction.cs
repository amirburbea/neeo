using System;

namespace Neeo.Sdk.Utilities;

internal static class IdentityFunction
{
    /// <summary>
    /// Gets an identity function for items of type <typeparamref name="TItem"/>.
    /// </summary>
    public static Func<TItem, TItem> For<TItem>() => Identity<TItem>.Function;

    private static class Identity<TItem>
    {
        public static readonly Func<TItem, TItem> Function = item => item;
    }
}