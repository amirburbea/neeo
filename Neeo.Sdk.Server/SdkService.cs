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
    TaskCompletionSource<ISdkEnvironment> environmentSource,
    ILogger<SdkService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Brain brain;
        try
        {
            brain = await GetBrainAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Brain discovery was cancelled!");
            return;
        }
        logger.LogInformation("Using Brain {Name} at {Endpoint}...", brain.HostName, brain.ServiceEndPoint);
        ISdkEnvironment environment = await brain.StartServerAsync([.. providers], name: configuration.GetValue<string>("ServerName"), cancellationToken: stoppingToken).ConfigureAwait(false);
        environmentSource.TrySetResult(environment);
        logger.LogInformation("Started server at address {Address}...", environment.HostAddress);
        TaskCompletionSource completionSource = new();
        stoppingToken.Register(async delegate
        {
            await environment.StopAsync(default).ConfigureAwait(false);
            completionSource.TrySetResult();
        });
        logger.LogInformation("Brain WebUI is running at http://{IPAddress}:3200/eui", brain.IPAddress);
        await completionSource.Task.ConfigureAwait(false);

        async ValueTask<Brain> GetBrainAsync()
        {
            if (configuration.GetValue<string>(nameof(Brain)) is { } text && IPAddress.TryParse(text, out IPAddress? address))
            {
                return new(address);
            }
            logger.LogInformation("Discovering Brain...");
            if (await Brain.DiscoverOneAsync(cancellationToken: stoppingToken).ConfigureAwait(false) is not { } brain)
            {
                throw new ApplicationException("Failed to discover Brain.");
            }
            return brain;
        }
    }
}
