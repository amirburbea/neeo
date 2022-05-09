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
    /// <param name="optionalDeviceId">The (optional) device identifier. If specified, it is expected that the method will return
    /// either an empty array or the single device requested.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to represent the asynchronous operation.</returns>
    Task<DiscoveredDevice[]> DiscoverAsync(string? optionalDeviceId = default, CancellationToken cancellationToken = default);
}

internal sealed class DiscoveryFeature : IDiscoveryFeature
{
    private readonly DiscoveryProcess _process;

    public DiscoveryFeature(DiscoveryProcess process, bool enableDynamicDeviceBuilder)
    {
        (this._process, this.EnableDynamicDeviceBuilder) = (process ?? throw new ArgumentNullException(nameof(process)), enableDynamicDeviceBuilder);
    }

    public bool EnableDynamicDeviceBuilder { get; }

    public async Task<DiscoveredDevice[]> DiscoverAsync(string? optionalDeviceId, CancellationToken cancellationToken)
    {
        DiscoveredDevice[] devices = await this._process(optionalDeviceId, cancellationToken).ConfigureAwait(false);
        if (devices is not { Length: 0 })
        {
            this.Validate(devices);
        }
        return devices;
    }

    private void Validate(DiscoveredDevice[] discoveredDevices)
    {
        HashSet<string> ids = new();
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