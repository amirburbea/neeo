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
    public static async Task<IHost> StartSdkAsync(SdkConfiguration configuration, IPAddress hostAddress, int port, bool consoleLogging, CancellationToken cancellationToken)
    {
        IHost host = new HostBuilder()
            .ConfigureWebHostDefaults(builder => Server.ConfigureWebHostDefaults(builder, hostAddress, port, consoleLogging))
            .ConfigureServices(services => Server.ConfigureServices(services, configuration))
            .Build();
        await host.StartAsync(cancellationToken);
        return host;
    }

    private static void ConfigureServices(IServiceCollection services, SdkConfiguration configuration)
    {
        services
            .AddSingleton(configuration)
            .AddSingleton<ISdkEnvironment, SdkEnvironment>()
            .AddSingleton<IApiClient, ApiClient>()
            .AddSingleton<IDeviceDatabase, DeviceDatabase>()
            .AddSingleton<IDynamicDevices, DynamicDevices>()
            .AddSingleton<IDynamicDeviceRegistrar, DynamicDevices>()
            .AddSingleton<INotificationMapping, NotificationMapping>()
            .AddSingleton<INotificationService, NotificationService>()
            .AddSingleton(PgpMethods.CreatePgpKeys()); // Creates new random PGP encryption keys.
        services
            .AddHostedService<SdkRegistration>()
            .AddHostedService<SubscriptionsNotifier>();
    }

    private static void ConfigureWebHostDefaults(IWebHostBuilder builder, IPAddress hostAddress, int port, bool consoleLogging) => builder
        .ConfigureKestrel((context, options) =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = Constants.MaxRequestBodySize;
            options.Listen(hostAddress, port);
            if (port != 0 && IPAddress.IsLoopback(hostAddress) && context.HostingEnvironment.IsDevelopment())
            {
                options.Listen(IPAddress.Loopback, port);
            }
        })
        .ConfigureLogging((context, builder) =>
        {
            builder.ClearProviders();
            if (consoleLogging)
            {
                builder.AddConsole();
            }
            if (context.HostingEnvironment.IsDevelopment())
            {
                builder.AddDebug();
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
        .Configure((context, builder) =>
        {
            builder
                .UseRouting()
                .UseCors(nameof(CorsPolicy))
                .UseEndpoints(endpoints => endpoints.MapControllers());
            if (context.HostingEnvironment.IsDevelopment())
            {
                builder.UseDeveloperExceptionPage();
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