using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// The single canonical <see cref="JsonSerializerOptions"/> instance used across Neeo.Sdk, combining
/// <see cref="AppJsonSerializerContext"/> (avoids reflection/emit for known wire types) with a
/// reflection-based fallback resolver (for the SDK's and drivers' open-generic call sites, whose
/// concrete types can't be enumerated at compile time). Naming policy and null-handling are inherited
/// directly from the generated context's own options, so there is exactly one place configuring them.
/// </summary>
internal static class AppJsonSerializerOptions
{
    public static readonly JsonSerializerOptions Default = new(AppJsonSerializerContext.Default.Options)
    {
        TypeInfoResolver = JsonTypeInfoResolver.Combine(AppJsonSerializerContext.Default, new DefaultJsonTypeInfoResolver()),
    };
}
