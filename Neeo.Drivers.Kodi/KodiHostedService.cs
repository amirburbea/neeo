using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk;
using Neeo.Sdk.Devices;

namespace Neeo.Drivers.Kodi;

public sealed class KodiHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IConfiguration _configuration;
    private readonly IDeviceProvider[] _deviceProviders;
    private readonly ILogger _logger;

    private Brain? _brain;

    public KodiHostedService(
        IConfiguration configuration,
        IEnumerable<IDeviceProvider> deviceProviders,
        IHostApplicationLifetime applicationLifetime,
        ILogger<KodiHostedService> logger
    )
    {
        (this._applicationLifetime, this._configuration, this._deviceProviders, this._logger) = (applicationLifetime, configuration, deviceProviders.ToArray(), logger);
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
                this._logger.LogError("Failed to resolve brain!");
                this._applicationLifetime.StopApplication();
                return;
            }
        }
        ISdkEnvironment environment = await brain.StartServerAsync(
            this._deviceProviders,
            configureLogging: static (_, builder) => builder.ClearProviders().AddSimpleConsole(options => options.SingleLine = true),
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);
        this._brain = brain;
        this._logger.LogInformation("Listening on {address}...", environment.HostAddress);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref this._brain, default) is { } brain)
        {
            await brain.StopServerAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}