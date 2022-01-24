using System;
using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Api.Json;

/// <summary>
/// Contains the <see langword="static"/> JSON serialization options to use with the NEEO Brain.
/// </summary>
public static class JsonSerialization
{
    /// <summary>
    /// An instance of <see cref="JsonSerializerOptions"/> suitable for interactions with the NEEO <see cref="Brain"/>.
    /// </summary>
    public static readonly JsonSerializerOptions Options = JsonSerialization.CreateJsonSerializerOptions();

    internal static void UpdateConfiguration(this JsonSerializerOptions options)
    {
        options.DictionaryKeyPolicy = options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        JsonSerializerOptions options = new(JsonSerializerDefaults.Web);
        options.UpdateConfiguration();
        return options;
    }
}