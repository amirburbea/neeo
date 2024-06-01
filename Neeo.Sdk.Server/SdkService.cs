using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
) : IHostedService
{
    private ISdkEnvironment? _sdkEnvironment;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Brain? brain;
        if (configuration.GetValue<string>(nameof(Brain)) is { } text && 
            IPAddress.TryParse(text, out IPAddress? address) &&
            address.AddressFamily == AddressFamily.InterNetwork)
        {
            brain = new(address);
        }
        else
        {
            logger.LogInformation("Discovering Brain...");
            if ((brain = await Brain.DiscoverOneAsync(cancellationToken).ConfigureAwait(false)) is null)
            {
                throw new ApplicationException("Failed to discover Brain. (If on Windows, ensure Bonjour is installed).");
            }
        }
        logger.LogInformation("Using Brain at {endpoint}...", brain.ServiceEndPoint);
        this._sdkEnvironment = await brain.StartServerAsync(
            providers as IDeviceProvider[] ?? providers.ToArray(),
            name: configuration.GetValue<string>("ServerName"),
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);
        logger.LogInformation("Started server at address {address}...", this._sdkEnvironment.HostAddress);
        if (Environment.UserInteractive && !Console.IsInputRedirected)
        {
            brain.OpenWebUI();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (this._sdkEnvironment is not { HostAddress: { } address } environment)
        {
            return Task.CompletedTask;
        }
        logger.LogInformation("Stopping server at address {address}...", address);
        return environment.StopAsync(cancellationToken);
    }
}
