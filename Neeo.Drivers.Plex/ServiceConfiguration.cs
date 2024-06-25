using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

public sealed class ServiceConfiguration : IServiceConfiguration
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<IPlexServerDiscovery, PlexServerDiscovery>()
            .AddSingleton<IPlexServerManager, PlexServerManager>()
            .AddSingleton<IPlexDriverSettings, PlexDriverSettings>()
            .AddHostedService(serviceProvider => (PlexDriverSettings)serviceProvider.GetRequiredService<IPlexDriverSettings>())
            .AddLogging(builder => builder.AddFilter((name, _) => name is null || !name.StartsWith(typeof(HttpClient).FullName!) && name != "Microsoft.Extensions.Http.DefaultHttpClientFactory"))
            .AddHttpClient(nameof(Plex), client => client.DefaultRequestHeaders.Accept.Add(new("application/json")))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All });
    }
}
