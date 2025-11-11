using Microsoft.Extensions.DependencyInjection;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Kodi;

public sealed class ServiceConfiguration : IServiceConfiguration
{
    public void ConfigureServices(IServiceCollection services) => services
        .AddSingleton<KodiClientManager>();
}
