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

    public bool EnableDynamicDeviceBuilder { get; } = enableDynamicDeviceBuilder;

    async Task<DiscoveredDevice?> IDiscoveryFeature.DiscoverAsync(string deviceId, CancellationToken cancellationToken)
    {
        return await this.DiscoverAsync(deviceId, cancellationToken).ConfigureAwait(false) is [DiscoveredDevice device]
            ? device
            : default;
    }

    Task<DiscoveredDevice[]> IDiscoveryFeature.DiscoverAsync(CancellationToken cancellationToken)
    {
        return this.DiscoverAsync(default, cancellationToken);
    }

    public async Task<DiscoveredDevice[]> DiscoverAsync(string? optionalDeviceId = default, CancellationToken cancellationToken = default)
    {
        if (await this._process(optionalDeviceId, cancellationToken).ConfigureAwait(false) is not { Length: > 0 } devices)
        {
            return [];
        }
        this.Validate(optionalDeviceId, devices);
        return devices;
    }

    /// <summary>
    /// Validate the array of discovered devices (with length not equal to zero).
    /// </summary>
    private void Validate(string? optionalDeviceId, DiscoveredDevice[] discoveredDevices)
    {
        if (optionalDeviceId != null && (discoveredDevices is not [{ Id: string deviceId }] || deviceId != optionalDeviceId))
        {
            throw new InvalidOperationException($"Discovery was to return at most one device with the id: {optionalDeviceId}");
        }
        HashSet<string> ids = new(discoveredDevices.Length);
        foreach ((string id, string name, _, _, IDeviceBuilder? device) in discoveredDevices)
        {
            if (string.IsNullOrEmpty(id) || !ids.Add(id))
            {
                throw new InvalidOperationException("Ids can not be null or blank and must be unique.");
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException("Names can not be null or blank.");
            }
            if (this.EnableDynamicDeviceBuilder == device is null)
            {
                throw new InvalidOperationException($"{name}: " + (device is null
                    ? "EnableDynamicDeviceBuilder was specified but a device was not supplied."
                    : "EnableDynamicDeviceBuilder was not specified but a device was supplied."
                ));
            }
        }
    }
}
