using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;

namespace Neeo.Drivers.Kodi;

internal static class Program
{
    private static void ConfigureLogging(HostBuilderContext context, ILoggingBuilder builder) => builder
        .ClearProviders()
        .AddDebug()
        .AddSimpleConsole(options => options.SingleLine = true);

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services) => services
        .AddSingleton<IDeviceProvider, KodiRemoteDeviceProvider>()
        .AddSingleton<IDeviceProvider, KodiPlayerDeviceProvider>()
        .AddSingleton<KodiClientManager>()
        .AddHostedService<KodiHostedService>();

    private static Task Main() => new HostBuilder()
        .ConfigureServices(Program.ConfigureServices)
        .ConfigureLogging(Program.ConfigureLogging)
        .RunConsoleAsync();
}