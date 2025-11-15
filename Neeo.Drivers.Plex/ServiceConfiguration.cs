using Microsoft.Extensions.DependencyInjection;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

public sealed class ServiceConfiguration : IServiceConfiguration
{
    public void ConfigureServices(IServiceCollection services) => services
        .AddSingleton<IPlexSettingsManager, PlexSettingsManager>()
        .AddSingleton<IPlexTokenStore, PlexTokenStore>()
        .AddSingleton<IPlexServerManager, PlexServerManager>()
        .AddSingleton<IPlexPlayerManagerFactory, PlexPlayerManager.Factory>()
        .AddHttpClient("plex", client => client.DefaultRequestHeaders.Accept.Add(new("application/json")));
}
