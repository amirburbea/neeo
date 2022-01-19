using System.Text.Json.Serialization;
using Neeo.Api.Devices.Discovery;

namespace Neeo.Api.Devices;

public interface IDeviceSetup
{
    bool? Discovery { get; }

    [JsonPropertyName("introtext")]
    string? DiscoveryDescription { get; }

    [JsonPropertyName("introheader")]
    string? DiscoveryHeaderText { get; }

    bool? EnableDynamicDeviceBuilder { get; }

    bool? Registration => this.RegistrationType.HasValue ? true : default(bool?);

    [JsonPropertyName("registrationText")]
    string? RegistrationDescription { get; }

    [JsonPropertyName("registrationHeader")]
    string? RegistrationHeaderText { get; }

    RegistrationType? RegistrationType { get; }
}

internal sealed class DeviceSetup : IDeviceSetup
{
    public bool? Discovery { get; set; }

    public string? DiscoveryDescription { get; set; }

    public string? DiscoveryHeaderText { get; set; }

    public bool? EnableDynamicDeviceBuilder { get; set; }

    public string? RegistrationDescription { get; set; }

    public string? RegistrationHeaderText { get; set; }

    public RegistrationType? RegistrationType { get; set; }
}