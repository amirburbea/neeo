using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Discovery;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Examples.Devices;

namespace Neeo.Sdk.Examples;

public sealed class SdkService : IHostedService
{
    private static readonly Regex _ipAddressRegex = new(@"^\d+\.\d+\.\d+\.\d+$");

    private readonly IDeviceBuilder[] _devices;
    private readonly ILogger<SdkService> _logger;
    private ISdkEnvironment? _environment;

    public SdkService(IEnumerable<IExampleDevice> examples, ILogger<SdkService> logger)
    {
        this._devices = examples.Select(exampleDevice => exampleDevice.Builder).ToArray();
        this._logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Brain? brain;
        if (Environment.GetCommandLineArgs() is { } args && args.Select((arg) => SdkService._ipAddressRegex.Match(arg.Trim())).FirstOrDefault(match => match.Success) is { } match)
        {
            brain = new(IPAddress.Parse(match.Value));
        }
        else
        {
            this._logger.LogInformation("Discovering Brain...");
            if ((brain = await BrainDiscovery.DiscoverAsync(cancellationToken: cancellationToken).ConfigureAwait(false)) is null)
            {
                throw new();
            }
        }
        this._environment = await brain.StartServerAsync(this._devices, "Example Service", consoleLogging: true, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref this._environment, null) is { } environment)
        {
            await environment.Brain.StopServerAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}