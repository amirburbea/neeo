using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Setup;

namespace Neeo.Sdk.Devices.Features;

/// <summary>
/// Feature support for device discovery.
/// </summary>
public interface IDiscoveryFeature : IFeature
{
    /// <summary>
    /// Gets a value indicating whether or not this device supports the creation of dynamic devices.
    /// </summary>
    bool EnableDynamicDeviceBuilder { get; }

    FeatureType IFeature.Type => FeatureType.Discovery;

    /// <summary>
    /// Asynchronously discover devices.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to represent the asynchronous operation.</returns>
    Task<DiscoveredDevice[]> DiscoverAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously discover a device with a specific identifier.
    /// </summary>
    /// <param name="deviceId">The device identifier requested.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to represent the asynchronous operation.</returns>
    Task<DiscoveredDevice?> DiscoverAsync(string deviceId, CancellationToken cancellationToken = default);
}

internal sealed class DiscoveryFeature(DiscoveryProcess process, bool enableDynamicDeviceBuilder = false) : IDiscoveryFeature
{
    private readonly DiscoveryProcess _process = process ?? throw new ArgumentNullException(nameof(process));

    public bool EnableDynamicDeviceBuilder => enableDynamicDeviceBuilder;

    async Task<DiscoveredDevice?> IDiscoveryFeature.DiscoverAsync(string deviceId, CancellationToken cancellationToken)
    {
        return await this.DiscoverAsync(deviceId, cancellationToken).ConfigureAwait(false) is [{ } device] ? device : default;
    }

    Task<DiscoveredDevice[]> IDiscoveryFeature.DiscoverAsync(CancellationToken cancellationToken) => this.DiscoverAsync(cancellationToken: cancellationToken);

    public async Task<DiscoveredDevice[]> DiscoverAsync(string? optionalDeviceId = default, CancellationToken cancellationToken = default)
    {
        if (await this._process(optionalDeviceId, cancellationToken).ConfigureAwait(false) is not { Length: > 0 } devices)
        {
            return [];
        }
        if (optionalDeviceId != null && (devices is not [{ Id: { } deviceId }] || deviceId != optionalDeviceId))
        {
            throw new InvalidOperationException($"Discovery was to return at most one device with the id: {optionalDeviceId}");
        }
        this.ValidateDevices(devices);
        return devices;
    }

    private void ValidateDevices(DiscoveredDevice[] devices)
    {
        HashSet<string> uniqueIds = new(devices.Length);
        foreach ((string deviceId, string name, _, _, IDeviceBuilder? builder) in devices)
        {
            if (string.IsNullOrEmpty(deviceId) || !uniqueIds.Add(deviceId))
            {
                throw new InvalidOperationException("Ids can not be null or blank and must be unique.");
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException("Names can not be null or blank.");
            }
            if (enableDynamicDeviceBuilder == builder is null)
            {
                throw new InvalidOperationException(
                    $"EnableDynamicDeviceBuilder was {(enableDynamicDeviceBuilder ? string.Empty : "not ")}specified " +
                    $"but a device was {(enableDynamicDeviceBuilder ? "not " : string.Empty)}supplied."
                );
            }
        }
    }
}
