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

namespace Neeo.Sdk.Server;

public sealed class SdkService(
        IEnumerable<IDeviceProvider> providers,
        IConfiguration configuration,
        ILogger<SdkService> logger
    ) : IHostedService
{
    private readonly IDeviceProvider[] _providers = providers as IDeviceProvider[] ?? providers.ToArray();
    private ISdkEnvironment? _environment;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Brain? brain;
        if (configuration.GetValue<string>(nameof(Brain)) is { } text && IPAddress.TryParse(text, out IPAddress? address))
        {
            brain = new(address);
        }
        else
        {
            logger.LogInformation("Discovering brain...");
            if ((brain = await Brain.DiscoverOneAsync(cancellationToken).ConfigureAwait(false)) is null)
            {
                const string errorMessage = "Failed to discover brain on the network. (If on Windows, ensure Bonjour is installed).";
                throw new ApplicationException(errorMessage);
            }
        }
        logger.LogInformation("Using brain at {endpoint}...", brain.ServiceEndPoint);
        this._environment = await brain.StartServerAsync(this._providers, name: configuration.GetValue<string>("ServerName"), cancellationToken: cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Started server at address {address}...", this._environment.HostAddress);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (this._environment is not { } environment)
        {
            return;
        }
        logger.LogInformation("Stopping server at address {address}...", environment.HostAddress);
        await environment.StopAsync(cancellationToken).ConfigureAwait(false);
    }
}