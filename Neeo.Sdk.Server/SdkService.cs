using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Server;

public sealed class SdkService(
    IEnumerable<IDeviceProvider> providers,
    IConfiguration configuration,
    ILogger<SdkService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Brain brain = await GetBrainAsync().ConfigureAwait(false);
        logger.LogInformation("Using Brain {name} at {endpoint}...", brain.HostName, brain.ServiceEndPoint);
        ISdkEnvironment environment = await brain.StartServerAsync(
            [.. providers],
            name: configuration.GetValue<string>("ServerName"),
            cancellationToken: stoppingToken
        ).ConfigureAwait(false);
        logger.LogInformation("Started server at address {address}...", environment.HostAddress);
        stoppingToken.Register(StopServerAsync);
        logger.LogInformation("Brain WebUI is running at http://{ipAddress}:3200/eui", brain.IPAddress);
        
        async ValueTask<Brain> GetBrainAsync()
        {
            if (configuration.GetValue<string>(nameof(Brain)) is { } text && IPAddress.TryParse(text, out IPAddress? address))
            {
                return new(address);
            }
            logger.LogInformation("Discovering Brain...");
            if (await Brain.DiscoverOneAsync(cancellationToken: stoppingToken).ConfigureAwait(false) is not { } brain)
            {
                throw new ApplicationException("Failed to discover Brain. (If on Windows, ensure Bonjour 3x is installed).");
            }
            return brain;
        }

        async void StopServerAsync()
        {
            logger.LogInformation("Stopping server at address {address}...", environment.HostAddress);
            await environment.StopAsync(default).ConfigureAwait(false);
        }
    }
}
