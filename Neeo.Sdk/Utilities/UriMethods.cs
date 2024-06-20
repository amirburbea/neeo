using System;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Contains simple extension methods for <see cref="Uri"/>.
/// </summary>
public static class UriMethods
{
    /// <summary>
    /// Creates a new <see cref="Uri"/> by combining the specified base URI and relative URI.
    /// </summary>
    /// <param name="baseUri">The base URI.</param>
    /// <param name="relativeUri">The relative URI to combine with the base URI.</param>
    /// <returns>The combined <see cref="Uri"/> instance.</returns>
    public static Uri Combine(this Uri baseUri, string relativeUri) => Uri.TryCreate(baseUri, relativeUri, out Uri? result)
        ? result
        : baseUri;

    /// <summary>
    /// Creates a new <see cref="Uri"/> by combining the specified base URI and relative URI.
    /// </summary>
    /// <param name="baseUri">The base URI.</param>
    /// <param name="relativeUri">The relative URI to combine with the base URI.</param>
    /// <returns>The combined <see cref="Uri"/> instance.</returns>
    public static Uri Combine(this Uri baseUri, Uri relativeUri) => Uri.TryCreate(baseUri, relativeUri, out Uri? result)
        ? result
        : baseUri;
}
