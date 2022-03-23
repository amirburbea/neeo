using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk;
using Neeo.Sdk.Devices;

namespace Neeo.Drivers.Kodi;

public class KodiHostedService : IHostedService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly IDeviceProvider[] _deviceProviders;
    private readonly ILogger _logger;

    private Brain? _brain;

    public KodiHostedService(IEnumerable<IDeviceProvider> deviceProviders, IHostApplicationLifetime applicationLifetime, ILogger<KodiHostedService> logger)
    {
        (this._applicationLifetime, this._deviceProviders, this._logger) = (applicationLifetime, deviceProviders.ToArray(), logger);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (await Brain.DiscoverOneAsync(cancellationToken: cancellationToken).ConfigureAwait(false) is not { } brain)
        {
            this._logger.LogInformation("Failed to resolve brain!");
            this._applicationLifetime.StopApplication();
            return;
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