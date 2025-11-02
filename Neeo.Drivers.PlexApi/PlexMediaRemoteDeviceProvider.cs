using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;

namespace Neeo.Drivers.PlexApi;

public sealed class PlexMediaRemoteDeviceProvider(
    IHttpClientFactory httpClientFactory,
    IPlexServerDiscovery discovery,
    IPlexTokenStore tokenStore,
    IPlexServerManager serverManager,
    ILogger<PlexMediaRemoteDeviceProvider> logger
) : PlexDeviceProviderBase(
    httpClientFactory,
    discovery,
    tokenStore,
    serverManager,
    logger,
    DeviceType.TV,
    "Media Remote"
)
{
    protected override IDeviceBuilder CreateDevice()
    {
        return base.CreateDevice()
            .AddSensor(Components.Playing.SensorName, null, (deviceId, _) => Task.FromResult(this.IsPlaying(deviceId)))
            .AddImageUrl(Components.CoverArt, "Cover Art", ImageSize.Large, this.GetCoverArtAsync)
            .AddTextLabel(Components.Description, "Description", this.GetDescriptionAsync)
            .AddTextLabel(Components.Title, "Title", this.GetTitleAsync)
            .AddDirectory("ROOT_DIRECTORY", "Menu", DirectoryRole.Root, this.BrowseRootDirectoryAsync, this.HandleRootDirectoryActionAsync)

            // TV requires an input, so we add one though it remains unused.
            .AddButton("INPUT PLEX");
    }
}
