using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Neeo.Api.Devices;

public interface IDeviceInfo
{
    DeviceIconOverride? Icon { get; }

    string Name { get; }

    [JsonPropertyName("specificname")]
    string? SpecificName { get; }

    IReadOnlyCollection<string> Tokens { get; }
}
