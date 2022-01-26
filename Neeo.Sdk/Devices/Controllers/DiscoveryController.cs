using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices.Discovery;

namespace Neeo.Sdk.Devices.Controllers;

public interface IDiscoveryController : IController
{
    ControllerType IController.Type => ControllerType.Discovery;

    Task<DiscoveryResult[]> DiscoverAsync(string? optionalDeviceId = default);
}

internal sealed class DiscoveryController : IDiscoveryController
{
    private readonly DiscoveryControllerFactory _discoveryControllerFactory;
    private readonly bool _enableDynamicDeviceBuilder;
    private readonly DiscoveryProcess _process;
    private readonly IDynamicDeviceRegistrar _registrar;

    public DiscoveryController(
        DiscoveryProcess process,
        bool enableDynamicDeviceBuilder,
        DiscoveryControllerFactory discoveryControllerFactory,
        IDynamicDeviceRegistrar registrar
    )
    {
        this._process = process;
        this._enableDynamicDeviceBuilder = enableDynamicDeviceBuilder;
        this._discoveryControllerFactory = discoveryControllerFactory;
        this._registrar = registrar;
    }

    public async Task<DiscoveryResult[]> DiscoverAsync(string? optionalDeviceId)
    {
        DiscoveryResult[] results = await this._process(optionalDeviceId).ConfigureAwait(false);
        if (results.Length == 0)
        {
            return results;
        }
        // Validate results first.
        HashSet<string> ids = new();
        List<(string, IDeviceBuilder)> builders = new();
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
            if (device == null)
            {
                continue;
            }
            if (!this._enableDynamicDeviceBuilder)
            {
                throw new InvalidOperationException("EnableDynamicDeviceBuilder was not specified.");
            }
            builders.Add((id, device));
        }
        // Build and register any dynamic devices.
        foreach ((string deviceId, IDeviceBuilder? builder) in builders)
        {
            this._registrar.RegisterDiscoveredDevice(deviceId, DeviceAdapter.Build(builder, this._discoveryControllerFactory));
        }
        return results;
    }
}

internal sealed class DiscoveryControllerFactory
{
    private readonly IDynamicDeviceRegistrar _registrar;

    public DiscoveryControllerFactory(IDynamicDeviceRegistrar registrar)
    {
        this._registrar = registrar;
    }

    public DiscoveryController CreateController(
        DiscoveryProcess process,
        bool enableDynamicDeviceBuilder
    ) => new(process, enableDynamicDeviceBuilder, this, this._registrar);
}