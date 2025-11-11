using System;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Drivers.Plex;

public readonly record struct PlexPlayerInfo(
    string Name,
    string MachineIdentifier,
    PlayerCapabilities Capabilities,
    [property: JsonConverter(typeof(PlexPlayerInfo.IPAddressConverter))] IPAddress IPAddress,
    int Port,
    string Product
)
{
    private sealed class IPAddressConverter : JsonConverter<IPAddress>
    {
        public override IPAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Expected string, got {reader.TokenType}");
            }
            return IPAddress.TryParse(reader.GetString()!, out IPAddress? ipAddress)
                ? ipAddress
                : throw new JsonException("Unable to parse IPAddress");
        }

        public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
