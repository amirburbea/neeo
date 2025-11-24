using Microsoft.Extensions.DependencyInjection;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

public sealed partial class ServiceConfiguration : IServiceConfiguration
{
    public void ConfigureServices(IServiceCollection services) => services
        .AddSingleton<IPlexServerManager, PlexServerManager>()
        .AddSingleton<IPlexSettingsManager, PlexSettingsManager>()
        .AddSingleton<IPlexTokenStore, PlexTokenStore>()
        .AddHttpClient(nameof(Plex), client => client.DefaultRequestHeaders.Accept.Add(new("application/json")))
        .ConfigurePrimaryHttpMessageHandler(PlexDirectConnect.CreateHttpHandler);
}
