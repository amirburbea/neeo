using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Drivers.Plex;

public readonly record struct PlexPlayerData(
    string Name,
    string MachineIdentifier,
    PlayerCapabilities Capabilities,
    [property: JsonConverter(typeof(PlexPlayerData.IPAddressConverter))] IPAddress IPAddress,
    int Port,
    string Product
)
{
    private sealed class IPAddressConverter : JsonConverter<IPAddress>
    {
        public override IPAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.TokenType switch
        {
            JsonTokenType.String when IPAddress.TryParse(reader.GetString(), out IPAddress? ipAddress) => ipAddress,
            _ => throw new JsonException($"Invalid JSON, expected string value of an IP address")
        };

        public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
    }
}
