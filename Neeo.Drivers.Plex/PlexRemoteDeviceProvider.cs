using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;

namespace Neeo.Drivers.Plex;

public sealed class PlexRemoteDeviceProvider(
    IHttpClientFactory httpClientFactory,
    IPlexServerDiscovery serverDiscovery,
    IPlexServerManager serverManager,
    IPlexTokenStore tokenStore,
    ILogger<PlexRemoteDeviceProvider> logger
) : PlexDeviceProviderBase(httpClientFactory, serverDiscovery, serverManager, tokenStore, logger, DeviceType.TV, "Remote")
{
    protected override IDeviceBuilder CreateDevice() => base.CreateDevice()
        .AddSensor(Components.Playing.SensorName, null, this.IsPlaying)
        .AddImageUrl(Components.CoverArt, "Cover Art", ImageSize.Large, this.GetCoverArt)
        .AddTextLabel(Components.Description, "Description", this.GetDescription)
        .AddTextLabel(Components.Title, "Title", this.GetTitle)
        .AddDirectory("ROOT_DIRECTORY", "Menu", DirectoryRole.Root, this.BrowseDirectoryAsync, this.HandleDirectoryActionAsync)
        .AddButton(PlexRemoteDeviceProvider.ButtonHandlers.Keys.Aggregate(default(Buttons), (x, y) => x | y))
        // TV requires an input, so we add one though it remains unused.
        .AddButton("INPUT PLEX");
}
