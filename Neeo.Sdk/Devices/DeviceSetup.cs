using System.Text.Json.Serialization;
using Neeo.Sdk.Devices.Discovery;

namespace Neeo.Sdk.Devices;

public sealed class DeviceSetup
{
    public bool? Discovery { get; internal set; }

    [JsonPropertyName("introtext")]
    public string? DiscoveryDescription { get; internal set; }

    [JsonPropertyName("introheader")]
    public string? DiscoveryHeaderText { get; internal set; }

    public bool? EnableDynamicDeviceBuilder { get; internal set; }

    public bool? Registration => this.RegistrationType.HasValue ? true : default(bool?);

    [JsonPropertyName("registrationText")]
    public string? RegistrationDescription { get; internal set; }

    [JsonPropertyName("registrationHeader")]
    public string? RegistrationHeaderText { get; internal set; }

    public RegistrationType? RegistrationType { get; internal set; }
}