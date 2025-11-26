using System;
using System.Collections.Generic;
using System.Text;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Provides identity functions that return their input unchanged for a specified type.
/// </summary>
/// <remarks>Use the <see cref="For{TItem}"/> method to obtain a reusable identity function for any type. This can
/// be useful in scenarios such as functional programming, LINQ queries, or when a default transformation is
/// required.</remarks>
public static class IdentityFunction
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
