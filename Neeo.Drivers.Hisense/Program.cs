using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Hisense;

internal static class Program
{
    //private static async Task Main()
    //{
    //   // await WakeOnLan.WakeAsync("18:30:0C:C3:F4:C8");
    //    //var dict = IPHelper.GetAllDevicesOnLAN();

    //    var devices = NetworkDevices.GetNetworkDevices();

    //    //if (await HisenseTV.DiscoverAsync() is not { } client)
    //    //{
    //    //    return;
    //    //}
    //    //await client.PublishAsync(new MqttApplicationMessage() { Topic = "ui.service", Payload = Encoding.UTF8.GetBytes("gettvstate") });
    //    //var r = await client.SubscribeAsync(new MQTTnet.Client.Subscribing.MqttClientSubscribeOptions()
    //    //{
    //    //    TopicFilters = { new() { Topic = "ui.service" } }
    //    //});

    //    //
    //}

    static async Task Main()
    {
        using IHost host = new HostBuilder()
            .UseConsoleLifetime() // Support stopping via CTRL+C.
            .ConfigureLogging(ConfigureLogging)
            .ConfigureServices(ConfigureServices)
            .Build();
        await host.StartAsync();
        await host.WaitForShutdownAsync();

        static void ConfigureLogging(ILoggingBuilder builder) => builder.ClearProviders().AddConsole();

        static void ConfigureServices(IServiceCollection services)
        {
            //services.AddHostedService<ExampleSdkService>();
            // Finds all device examples and registers them.
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsAssignableTo(typeof(IDeviceProvider)) && !type.IsInterface && !type.IsAbstract)
                {
                    services.AddSingleton(typeof(IDeviceProvider), type);
                }
            }
        }
    }
}