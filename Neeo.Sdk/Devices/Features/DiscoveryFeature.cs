using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Neeo.Sdk.Devices.Discovery;

namespace Neeo.Sdk.Devices.Features;

public interface IDiscoveryFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Discovery;

    bool EnableDynamicDeviceBuilder { get; }

    Task<DiscoveredDevice[]> DiscoverAsync(string? optionalDeviceId = default);
}

internal sealed class DiscoveryFeature : IDiscoveryFeature
{
    private readonly DiscoveryProcess _process;

    public bool EnableDynamicDeviceBuilder { get; }

    public DiscoveryFeature(DiscoveryProcess process, bool enableDynamicDeviceBuilder)
    {
        (this._process, this.EnableDynamicDeviceBuilder) = (process??throw new ArgumentNullException(nameof(process)), enableDynamicDeviceBuilder);
    }

    public async Task<DiscoveredDevice[]> DiscoverAsync(string? optionalDeviceId)
    {
        DiscoveredDevice[] results = await this._process(optionalDeviceId).ConfigureAwait(false);
        if (results.Length == 0)
        {
            return results;
        }
        // Validate results first.
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
            if (device != null && !this.EnableDynamicDeviceBuilder)
            {
                throw new InvalidOperationException("EnableDynamicDeviceBuilder was not specified.");
            }
        }
        return results;
    }
}