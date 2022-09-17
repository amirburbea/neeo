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
    private ISdkEnvironment? _environment;

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
        if (await this.GetBrainAsync(cancellationToken).ConfigureAwait(false) is not { } brain)
        {
            this._applicationLifetime.StopApplication();
            return;
        }
        this._environment = await brain.StartServerAsync(
            this._deviceProviders,
            configureLogging: static (_, builder) => builder.ClearProviders().AddSimpleConsole(options => options.SingleLine = true),
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);
        this._logger.LogInformation("Listening on {address}...", this._environment.HostAddress);
    }

    public Task StopAsync(CancellationToken cancellationToken) => this._environment?.StopAsync(cancellationToken) ?? Task.CompletedTask;

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
}