using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Neeo.Discovery;
using Neeo.Sdk;
using Neeo.Sdk.Devices;

namespace Remote.HodgePodge;

public sealed class ExampleNeeoService : IHostedService
{
    private readonly IDeviceBuilder[] _devices;
    private ISdkEnvironment? _environment;

    public ExampleNeeoService(IEnumerable<IExampleDeviceProvider> providers)
    {
        this._devices = providers.Select(provider => provider.ProvideDevice()).ToArray();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (await BrainDiscovery.DiscoverAsync(cancellationToken: cancellationToken).ConfigureAwait(false) is { } brain)
        {
            this._environment = await brain.StartServerAsync(this._devices, "Example Service", cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref this._environment, null) is { } environment)
        {
            await environment.Brain.StopServerAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}