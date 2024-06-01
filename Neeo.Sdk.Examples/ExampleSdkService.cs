using System.Collections.Generic;
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
public sealed class ExampleSdkService(IConfiguration configuration, IEnumerable<IDeviceProvider> providers, ILogger<ExampleSdkService> logger, IHostApplicationLifetime applicationLifetime) : IHostedService
{
    private ISdkEnvironment? _environment;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (await this.GetBrainAsync(cancellationToken).ConfigureAwait(false) is not { } brain)
        {
            applicationLifetime.StopApplication();
            return;
        }
        logger.LogInformation("Starting SDK with Brain {ip}...", brain.IPAddress);
        this._environment = await brain.StartServerAsync(
            [.. providers],
            configureLogging: static (context, builder) => builder.AddSimpleConsole(static options => options.SingleLine = true),
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);
        logger.LogInformation("Listening on {address}...", this._environment.HostAddress);
    }

    public Task StopAsync(CancellationToken cancellationToken) => this._environment?.StopAsync(cancellationToken) ?? Task.CompletedTask;

    private async ValueTask<Brain?> GetBrainAsync(CancellationToken cancellationToken)
    {
        if (configuration["NEEO_BRAIN"] is { } text && IPAddress.TryParse(text, out IPAddress? ipAddress))
        {
            return new(ipAddress);
        }
        logger.LogInformation("Discovering Brain...");
        if (await Brain.DiscoverOneAsync(cancellationToken: cancellationToken).ConfigureAwait(false) is { } brain)
        {
            return brain;
        }
        logger.LogError("Failed to resolve brain!");
        return null;
    }
}
