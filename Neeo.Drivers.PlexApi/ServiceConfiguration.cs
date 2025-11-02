using Microsoft.Extensions.DependencyInjection;
using Neeo.Sdk.Server.Drivers;

namespace Neeo.Drivers.PlexApi;

public sealed class ServiceConfiguration : IServiceConfiguration
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<IPlexServerDiscovery, PlexServerDiscovery>()
            .AddSingleton<IPlexSettingsManager, PlexSettingsManager>()
            .AddSingleton<IPlexTokenStore, PlexTokenStore>()
            .AddSingleton<IPlexServerManager, PlexServerManager>()
            .AddHttpClient("plex", client => client.DefaultRequestHeaders.Accept.Add(new("application/json")));
    }
}
