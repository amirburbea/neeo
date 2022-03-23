using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Discovery;

namespace Neeo.Sdk.Devices.Features;

/// <summary>
/// Feature support for device discovery.
/// </summary>
public interface IDiscoveryFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Discovery;

    /// <summary>
    /// Gets a value indicating whether or not this device supports the creation of dynamic devices.
    /// </summary>
    bool EnableDynamicDeviceBuilder { get; }

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

    public bool EnableDynamicDeviceBuilder { get; }

    public DiscoveryFeature(DiscoveryProcess process, bool enableDynamicDeviceBuilder)
    {
        (this._process, this.EnableDynamicDeviceBuilder) = (process ?? throw new ArgumentNullException(nameof(process)), enableDynamicDeviceBuilder);
    }

    public async Task<DiscoveredDevice[]> DiscoverAsync(string? optionalDeviceId, CancellationToken cancellationToken)
    {
        DiscoveredDevice[] results = await this._process(optionalDeviceId, cancellationToken).ConfigureAwait(false);
        if (results.Length != 0)
        {
            // Validate results first.
            this.Validate(results);
        }
        return results;
    }

    private void Validate(DiscoveredDevice[] results)
    {
        HashSet<string> ids = new();
        foreach ((string id, string name, _, _, IDeviceBuilder? device) in results)
        {
            if (string.IsNullOrEmpty(id) || !ids.Add(id))
            {
                throw new InvalidOperationException("Ids can not be null or blank and must be unique.");
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException("Names can not be null or blank.");
            }
            if (this.EnableDynamicDeviceBuilder != (device is { }))
            {
                throw new InvalidOperationException(device != null
                    ? "EnableDynamicDeviceBuilder was not specified but a device was supplied."
                    : "EnableDynamicDeviceBuilder was specified but a device was not supplied."
                );
            }
        }
    }
}