using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices;

public interface IDeviceInfo
{
    DeviceIconOverride? Icon { get; }

    string Name { get; }

    [JsonPropertyName("specificname")]
    string? SpecificName { get; }

    IReadOnlyCollection<string> Tokens { get; }
}
