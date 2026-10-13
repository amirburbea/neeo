using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

[assembly: InternalsVisibleTo("Neeo.Sdk.Tests")]

namespace Neeo.Drivers.WebOS;

internal static class WebOSJsonSerializerOptions
{
    public static readonly JsonSerializerOptions Default = new(WebOSJsonSerializerContext.Default.Options)
    {
        TypeInfoResolver = JsonTypeInfoResolver.Combine(
            WebOSJsonSerializerContext.Default,
            new DefaultJsonTypeInfoResolver()
        ),
    };
}
