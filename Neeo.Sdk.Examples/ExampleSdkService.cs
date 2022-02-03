using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Discovery;

namespace Neeo.Sdk.Examples;

/// <summary>
/// Starts the SDK with all the examples.
/// </summary>
public sealed class ExampleSdkService : IHostedService
{
    private static readonly Regex _ipAddressRegex = new(@"^\d+\.\d+\.\d+\.\d+$");

    private readonly ILogger<ExampleSdkService> _logger;
    private readonly IDeviceProvider[] _providers;
    private ISdkEnvironment? _environment;

    public ExampleSdkService(IEnumerable<IDeviceProvider> providers, ILogger<ExampleSdkService> logger)
    {
        (this._providers, this._logger) = (providers.ToArray(), logger);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Brain? brain;
        if (Environment.GetCommandLineArgs() is { } args && args.Select((arg) => ExampleSdkService._ipAddressRegex.Match(arg.Trim())).FirstOrDefault(match => match.Success) is { Value: { } text })
        {
            brain = new(IPAddress.Parse(text));
        }
        else
        {
            this._logger.LogInformation("Discovering Brain...");
            if ((brain = await Brains.DiscoverAsync(cancellationToken: cancellationToken).ConfigureAwait(false)) is null)
            {
                throw new InvalidOperationException("Brain not found.");
            }
        }
        this._logger.LogInformation("Starting up...");
        this._environment = await brain.StartServerAsync(this._providers, "Example Service", cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref this._environment, null) is not { } environment)
        {
            return;
        }
        this._logger.LogInformation("Shutting down...");
        await environment.Brain.StopServerAsync(cancellationToken).ConfigureAwait(false);
    }
}