using System.Net.Http;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;

namespace Neeo.Drivers.PlexApi;

public class PlexRemoteDeviceProvider(
    IHttpClientFactory httpClientFactory,
    IPlexDiscovery discovery,
    IPlexTokenStore tokenStore,
    IPlexServerManager serverManager,
    ILogger<PlexRemoteDeviceProvider> logger
) : PlexDeviceProviderBase(
    httpClientFactory,
    discovery,
    tokenStore,
    serverManager,
    logger,
    "Remote",
    DeviceType.TV
)
{
    protected override IDeviceBuilder CreateDevice()
    {
        return base.CreateDevice()
            // TV requires an input, so we add one though it remains unused.
            .AddButton("INPUT Plex", "Plex");
    }
}