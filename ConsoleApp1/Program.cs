using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleApp1;

internal static class Program
{
    private static async Task Main()
    {
        using IHost host = new HostBuilder()
            .UseConsoleLifetime() // Support stopping via CTRL+C.
            .ConfigureLogging(ConfigureLogging)
            .ConfigureServices(ConfigureServices)
            .Build();
        await host.StartAsync();
        await host.WaitForShutdownAsync();

        static void ConfigureLogging(ILoggingBuilder builder) => builder.ClearProviders().AddConsole();

        static void ConfigureServices(IServiceCollection services) => services.AddHostedService<HisenseService>();
    }
}