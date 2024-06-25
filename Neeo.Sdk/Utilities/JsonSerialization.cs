using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Contains the <see langword="static"/> JSON serialization options for interactions with NodeJS or the NEEO Brain.
/// </summary>
public static class JsonSerialization
{
    /// <summary>
    /// <see cref="JsonSerializerOptions"/> suitable for interactions with web-based scenarios.
    /// </summary>
    public static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
