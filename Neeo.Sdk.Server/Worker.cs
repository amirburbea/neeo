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

public sealed class Worker : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<Worker> _logger;
    private readonly IDeviceProvider[] _providers;
    private ISdkEnvironment? _environment;

    public Worker(
        IEnumerable<IDeviceProvider> providers,
        IConfiguration configuration,
        ILogger<Worker> logger
    ) => (this._configuration, this._logger, this._providers) = (configuration, logger, providers as IDeviceProvider[] ?? providers.ToArray());

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Brain? brain;
        if (this._configuration.GetValue<string>(nameof(Brain)) is { } text && IPAddress.TryParse(text, out IPAddress? address))
        {
            brain = new(address);
        }
        else
        {
            this._logger.LogInformation("Discovering brain...");
            if ((brain = await Brain.DiscoverOneAsync(cancellationToken).ConfigureAwait(false)) is null)
            {
                const string errorMessage = "Failed to discover brain on the network. (If on Windows, ensure Bonjour is installed).";
                throw new ApplicationException(errorMessage);
            }
        }
        this._logger.LogInformation("Using brain at {endpoint}...", brain.ServiceEndPoint);
        this._environment = await brain.StartServerAsync(this._providers, name: this._configuration.GetValue<string>("ServerName"), cancellationToken: cancellationToken).ConfigureAwait(false);
        this._logger.LogInformation("Started server at host address {address}...", this._environment.HostAddress);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await (this._environment?.StopAsync(cancellationToken) ?? Task.CompletedTask).ConfigureAwait(false);
    }
}