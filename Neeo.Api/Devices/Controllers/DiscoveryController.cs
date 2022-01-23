using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Api.Devices.Discovery;

namespace Neeo.Api.Devices.Controllers;

public interface IDiscoveryController : IController
{
    ControllerType IController.Type => ControllerType.Discovery;

    Task<DiscoveryResult[]> DiscoverAsync(string? optionalDeviceId = default);
}

internal sealed class DiscoveryController : IDiscoveryController
{
    private readonly DiscoveryControllerFactory _discoveryControllerFactory;
    private readonly ILogger<DiscoveryController> _logger;
    private readonly DiscoveryProcess _process;
    private readonly IDynamicDeviceRegistrar _registrar;

    public DiscoveryController(
        DiscoveryProcess process,
        DiscoveryControllerFactory discoveryControllerFactory,
        IDynamicDeviceRegistrar registrar,
        ILogger<DiscoveryController> logger
    )
    {
        this._process = process;
        this._discoveryControllerFactory = discoveryControllerFactory;
        this._registrar = registrar;
        this._logger = logger;
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
    private readonly ILogger<DiscoveryController> _logger;
    private readonly IDynamicDeviceRegistrar _registrar;

    public DiscoveryControllerFactory(IDynamicDeviceRegistrar registrar, ILogger<DiscoveryController> logger)
    {
        this._registrar = registrar;
        this._logger = logger;
    }

    public DiscoveryController CreateController(DiscoveryProcess process) => new(process, this, this._registrar, this._logger);
}