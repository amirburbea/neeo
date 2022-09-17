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
    private readonly IHostApplicationLifetime _applicationLifetime;
    private ISdkEnvironment? _environment;

    public ExampleSdkService(IConfiguration configuration, IEnumerable<IDeviceProvider> providers, ILogger<ExampleSdkService> logger,IHostApplicationLifetime applicationLifetime)
    {
        (this._configuration, this._providers, this._logger,this._applicationLifetime) = (configuration, providers.ToArray(), logger, applicationLifetime);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (await this.GetBrainAsync(cancellationToken).ConfigureAwait(false) is not { } brain)
        {
            this._applicationLifetime.StopApplication();
            return;
        }
        this._logger.LogInformation("Starting SDK with Brain {ip}...", brain.IPAddress);
        this._environment = await brain.StartServerAsync(
            this._providers,
            configureLogging: static (context, builder) => builder.AddSimpleConsole(static options => options.SingleLine = true),
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);
        this._logger.LogInformation("Listening on {address}...", this._environment.HostAddress);
    }

    public Task StopAsync(CancellationToken cancellationToken) =>  this._environment?.StopAsync(cancellationToken) ?? Task.CompletedTask;

    private async ValueTask<Brain?> GetBrainAsync(CancellationToken cancellationToken)
    {
        if (this._configuration["NEEO_BRAIN"] is { } text && IPAddress.TryParse(text, out IPAddress? ipAddress))
        {
            return new(ipAddress);
        }
        this._logger.LogInformation("Discovering Brain...");
        if (await Brain.DiscoverOneAsync(cancellationToken: cancellationToken).ConfigureAwait(false) is { } brain)
        {
            return brain;
        }
        this._logger.LogError("Failed to resolve brain!");
        return null;
    }

    /*
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
        await (this._brain = brain).StartServerAsync(
            this._providers,
            "Examples",
            configureLogging: static (context, builder) => builder.AddSimpleConsole(static options => options.SingleLine = true),
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Brain? brain = Interlocked.Exchange(ref this._brain, null);
        await (brain?.StopServerAsync(cancellationToken) ?? Task.CompletedTask).ConfigureAwait(false);
    }
    */
}