using System.Text.Json.Serialization;
using Neeo.Sdk.Devices.Discovery;

namespace Neeo.Sdk.Devices;

public sealed class DeviceSetup
{
    public bool? Discovery { get; internal set; }

    [JsonPropertyName("introheader")]
    public string? DiscoveryHeaderText { get; internal set; }

    [JsonPropertyName("introtext")]
    public string? DiscoverySummary { get; internal set; }

    public bool? EnableDynamicDeviceBuilder { get; internal set; }

    public bool? Registration => this.RegistrationType.HasValue ? true : default(bool?);

    [JsonPropertyName("registrationHeader")]
    public string? RegistrationHeaderText { get; internal set; }

    [JsonPropertyName("registrationText")]
    public string? RegistrationSummary { get; internal set; }

    public RegistrationType? RegistrationType { get; internal set; }
}