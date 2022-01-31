using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Json;
using Neeo.Sdk.Notifications;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Rest;

internal static class Server
{
    public static async Task<IHost> StartSdkAsync(SdkConfiguration configuration, IPAddress hostAddress, int port, CancellationToken cancellationToken = default)
    {
        IHost host = new HostBuilder()
            .ConfigureWebHostDefaults(builder => Server.ConfigureWebHostDefaults(builder, hostAddress, port))
            .ConfigureServices(services =>
            {
                // Dependencies.
                services
                    .AddSingleton(configuration)
                    .AddSingleton<ISdkEnvironment, SdkEnvironment>()
                    .AddSingleton<IApiClient, ApiClient>()
                    .AddSingleton<IDeviceDatabase, DeviceDatabase>()
                    .AddSingleton<IDynamicDevices, DynamicDevices>()
                    .AddSingleton<IDynamicDeviceRegistrar, DynamicDevices>()
                    .AddSingleton<INotificationMapping, NotificationMapping>()
                    .AddSingleton<INotificationService, NotificationService>()
                    .AddSingleton(PgpMethods.CreatePgpKeys());

                // Startup and shut down tasks.
                services
                    .AddHostedService<SdkRegistration>()
                    .AddHostedService<SubscriptionsNotifier>();
            })
            .Build();
        await host.StartAsync(cancellationToken);
        return host;
    }

    private static void ConfigureWebHostDefaults(IWebHostBuilder builder, IPAddress hostAddress, int port) => builder
        .ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = Constants.MaxRequestBodySize;
            options.Listen(hostAddress, port);
        })
        .ConfigureLogging((context, logBuilder) =>
        {
            logBuilder
                .ClearProviders()
                .AddConsole();
            if (context.HostingEnvironment.IsDevelopment())
            {
                logBuilder.AddDebug();
            }
        })
        .ConfigureServices((context, services) =>
        {
            services
                .AddMvcCore(options => options.AllowEmptyInputInBodyModelBinding = true)
                .AddCors(options => options.AddPolicy(nameof(CorsPolicy), builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()))
                .AddJsonOptions(options => options.JsonSerializerOptions.UpdateConfiguration())
                .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(AssemblyControllerFeatureProvider.Instance));
        })
        .Configure((context, appBuilder) =>
        {
            appBuilder
                .UseRouting()
                .UseCors(nameof(CorsPolicy))
                .UseEndpoints(endpoints => endpoints.MapControllers());
            if (context.HostingEnvironment.IsDevelopment())
            {
                appBuilder.UseDeveloperExceptionPage();
            }
        });

    private static class Constants
    {
        public const int MaxRequestBodySize = 2 * 1024 * 1024;
    }

    private sealed class AssemblyControllerFeatureProvider : ControllerFeatureProvider
    {
        public static readonly ControllerFeatureProvider Instance = new AssemblyControllerFeatureProvider();

        protected override bool IsController(TypeInfo info) => info.Assembly == this.GetType().Assembly && info.IsAssignableTo(typeof(ControllerBase));
    }
}