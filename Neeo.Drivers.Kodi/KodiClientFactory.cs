using System.Net;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Kodi;

[Service]
public sealed class KodiClientFactory(ILogger<KodiClient> clientLogger)
{
    public KodiClient CreateClient(string displayName, IPAddress ipAddress, int httpPort) => new(displayName, ipAddress, httpPort, clientLogger);
}