using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Contains the <see langword="static"/> JSON serialization options to use with the NEEO Brain.
/// </summary>
public static class JsonSerialization
{
    /// <summary>
    /// <see cref="JsonSerializerOptions"/> suitable for interactions with the NEEO <see cref="Brain"/>.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
