using Microsoft.Extensions.DependencyInjection;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.WebOS;

public sealed class ServiceConfiguration : IServiceConfiguration
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<WebOSClientManager>();
    }
}
