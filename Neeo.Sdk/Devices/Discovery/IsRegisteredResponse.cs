using System.Text.Json.Serialization;

namespace Neeo.Sdk.Devices.Discovery;

public readonly struct IsRegisteredResponse
{
    public IsRegisteredResponse(bool isRegistered) => this.IsRegistered = isRegistered;

    [JsonPropertyName("registered")]
    public bool IsRegistered { get; }
}