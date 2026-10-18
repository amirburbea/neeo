using System;
using System.Text.Json.Serialization.Metadata;

namespace Neeo.Sdk.Utilities;

internal sealed partial class AppJsonSerializerContext
{
    /// <summary>
    /// Gets the source-generated metadata for <typeparamref name="T"/>, failing with an actionable message
    /// (rather than falling back to reflection, which is unavailable under AOT) if the type is not registered.
    /// </summary>
    public static JsonTypeInfo<T> TypeInfo<T>() => AppJsonSerializerContext.Default.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
        ?? throw new InvalidOperationException($"No JSON metadata is registered for '{typeof(T)}'. Add a [JsonSerializable] attribute for it to {nameof(AppJsonSerializerContext)}.");
}
