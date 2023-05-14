using System.Net;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Kodi;

[Service]
public sealed class KodiClientFactory
{
    private readonly ILogger<KodiClient> _clientLogger;

    public KodiClientFactory(ILogger<KodiClient> clientLogger) => this._clientLogger = clientLogger;

    public KodiClient CreateClient(string displayName, IPAddress ipAddress, int httpPort) => new(displayName, ipAddress, httpPort, this._clientLogger);
}