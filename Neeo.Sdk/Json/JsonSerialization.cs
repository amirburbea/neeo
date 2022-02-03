using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Sdk.Json;

/// <summary>
/// Contains the <see langword="static"/> JSON serialization options to use with the NEEO Brain.
/// </summary>
public static class JsonSerialization
{
    /// <summary>
    /// <see cref="JsonSerializerOptions"/> suitable for interactions with the NEEO <see cref="Brain"/>.
    /// </summary>
    public static readonly JsonSerializerOptions Options = JsonSerialization.CreateJsonSerializerOptions();

    /// <summary>
    /// Converts the <see cref="JsonElement" /> representing a single JSON value into a <typeparamref name="TValue" />.
    /// </summary>
    /// <typeparam name="TValue">The type to deserialize the JSON value into.</typeparam>
    /// <param name="element">The JSON element to deserialize.</param>
    /// <returns>The <typeparamref name="TValue"/> representation of the JSON value.</returns>
    public static TValue Deserialize<TValue>(this JsonElement element) => element.Deserialize<TValue>(JsonSerialization.Options)!;

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