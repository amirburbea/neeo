using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk;
using Neeo.Sdk.Devices;

namespace Neeo.Drivers.Plex;

public sealed class PlexRemoteDeviceProvider(
    IHttpClientFactory httpClientFactory,
    IPlexServerManager serverManager,
    IPlexTokenStore tokenStore,
    Task<ISdkEnvironment> startupTask,
    ILogger<PlexRemoteDeviceProvider> logger
) : PlexDeviceProviderBase(
    httpClientFactory,
    serverManager, 
    tokenStore, 
    startupTask, 
    logger, 
    DeviceType.TV, 
    "Remote"
)
{
    protected override IDeviceBuilder CreateDevice() => base.CreateDevice()
        .AddSensor(Components.Playing.SensorName, null, this.IsPlaying)
        .AddImageUrl(Components.CoverArt, "Cover Art", ImageSize.Large, this.GetCoverArt)
        .AddTextLabel(Components.Description, "Description", this.GetDescription)
        .AddTextLabel(Components.Title, "Title", this.GetTitle)
        .AddDirectory(Components.RootDirectory, "Menu", DirectoryRole.Root, this.BrowseDirectoryAsync, this.HandleDirectoryActionAsync)
        .AddButton(PlexRemoteDeviceProvider.ButtonHandlers.Keys.Aggregate(default(Buttons), (x, y) => x | y))
        // TV requires an input, so we add one though it remains unused.
        .AddButton("INPUT PLEX");
}
