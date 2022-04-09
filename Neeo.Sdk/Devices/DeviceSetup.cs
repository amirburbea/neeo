using System.Text.Json.Serialization;
using Neeo.Sdk.Devices.Setup;

namespace Neeo.Sdk.Devices;

/// <summary>
/// Contains information relating to device setup, specifically registration and discovery.
/// </summary>
public sealed class DeviceSetup
{
    /// <summary>
    /// Gets a value indicating if discovery is enabled.
    /// <para />
    /// A value of <see langword="null"/> is equivalent to <see langword="false"/>.
    /// </summary>
    public bool? Discovery { get; internal set; }

    /// <summary>
    /// Gets the header text to display when the discovery process is initiated, (returns <see langword="null"/> if discovery is not enabled).
    /// </summary>
    [JsonPropertyName("introheader")]
    public string? DiscoveryHeaderText { get; internal set; }

    /// <summary>
    /// Gets the summary text to display when the discovery process is initiated, (returns <see langword="null"/> if discovery is not enabled).
    /// </summary>
    [JsonPropertyName("introtext")]
    public string? DiscoverySummary { get; internal set; }

    /// <summary>
    /// If discovery is configured for the device, gets a value indicating if &quot;enableDynamicDeviceBuilder&quot; is enabled.
    /// <para />
    /// A value of <see langword="null"/> is equivalent to <see langword="false"/>.
    /// </summary>
    public bool? EnableDynamicDeviceBuilder { get; internal set; }

    /// <summary>
    /// Gets a value indicating if registration is enabled.
    /// <para />
    /// A value of <see langword="null"/> is equivalent to <see langword="false"/>.
    /// </summary>
    public bool? Registration => this.RegistrationType.HasValue ? true : default(bool?);

    /// <summary>
    /// Gets the header text to display when a user is entering registration credentials, (returns <see langword="null"/> if registration is not enabled).
    /// </summary>
    [JsonPropertyName("registrationHeader")]
    public string? RegistrationHeaderText { get; internal set; }

    /// <summary>
    /// Gets the summary text to display when a user is entering registration credentials, (returns <see langword="null"/> if registration is not enabled).
    /// </summary>
    [JsonPropertyName("registrationText")]
    public string? RegistrationSummary { get; internal set; }

    /// <summary>
    /// Gets the type of registration if it is configured on the device (returns <see langword="null"/> otherwise).
    /// </summary>
    public RegistrationType? RegistrationType { get; internal set; }
}