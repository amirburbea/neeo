using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Drivers.Hisense;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Examples;

public static class Program
{
    private static readonly Assembly[] _assemblies = new[]
    {
        Assembly.GetExecutingAssembly(),
        typeof(HisenseDeviceProvider).Assembly,
    };

    private static async Task Main()
    {
        await new HostBuilder()
            .ConfigureDefaults(Environment.GetCommandLineArgs())
            .ConfigureLogging(ConfigureLogging)
            .ConfigureServices(ConfigureServices)
            .RunConsoleAsync().ConfigureAwait(false);

        static void ConfigureLogging(ILoggingBuilder builder) => builder.ClearProviders().AddSimpleConsole(static options => options.SingleLine = true);

        static void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedService<ExampleSdkService>();
            // Finds all device examples and registers them.
            foreach (Type type in Program._assemblies.Distinct().SelectMany(static assembly => assembly.GetTypes()))
            {
                if (type.IsAssignableTo(typeof(IDeviceProvider)) && !type.IsInterface && !type.IsAbstract)
                {
                    services.AddSingleton(typeof(IDeviceProvider), type);
                }
            }
        }
    }
}