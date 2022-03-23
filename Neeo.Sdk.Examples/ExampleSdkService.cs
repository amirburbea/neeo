using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Examples;

/// <summary>
/// Starts the SDK with all the examples.
/// </summary>
public sealed class ExampleSdkService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExampleSdkService> _logger;
    private readonly IDeviceProvider[] _providers;
    private Brain? _brain;

    public ExampleSdkService(IConfiguration configuration, IEnumerable<IDeviceProvider> providers, ILogger<ExampleSdkService> logger)
    {
        (this._configuration, this._providers, this._logger) = (configuration, providers.ToArray(), logger);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // You can set environment variable DOTNET_NEEO_BRAIN to an IPv4 address to skip discovery and use the specified brain.
        Brain? brain;
        if (this._configuration["NEEO_BRAIN"] is { } text && IPAddress.TryParse(text, out IPAddress? ipAddress))
        {
            brain = new(ipAddress);
        }
        else
        {
            this._logger.LogInformation("Discovering Brain...");
            if ((brain = await Brain.DiscoverOneAsync(cancellationToken: cancellationToken).ConfigureAwait(false)) is null)
            {
                throw new InvalidOperationException("Brain not found.");
            }
        }
        this._logger.LogInformation("Starting SDK with Brain {ip}...", brain.IPAddress);
        await (this._brain = brain).StartServerAsync(this._providers, "Examples", cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Brain? brain = Interlocked.Exchange(ref this._brain, null);
        await (brain?.StopServerAsync(cancellationToken) ?? Task.CompletedTask).ConfigureAwait(false);
    }
}