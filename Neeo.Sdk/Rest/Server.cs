using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Notifications;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Rest;

internal static class Server
{
    public static async Task<IHost> StartSdkAsync(
        Brain brain,
        Func<IServiceProvider, IReadOnlyCollection<IDeviceBuilder>> devices,
        string adapterName,
        IPAddress hostAddress,
        int port,
        IServiceConfiguration[]? serviceConfigurations = default,
        Action<HostBuilderContext, ILoggingBuilder>? configureLogging = default,
        CancellationToken cancellationToken = default
    )
    {
        IHost host = new HostBuilder()
            .ConfigureWebHostDefaults(builder => Server.ConfigureWebHostDefaults(builder, hostAddress, port))
            .ConfigureLogging(configureLogging ?? Server.ConfigureLoggingDefaults)
            .ConfigureServices(Server.ConfigureHttpClient)
            .ConfigureServices(services => Server.ConfigureServices(services, brain, devices, adapterName, serviceConfigurations))
            .Build();
        await host.StartAsync(cancellationToken).ConfigureAwait(false);
        return host;
    }

    private static void ConfigureHttpClient(IServiceCollection services)
    {
        void AddBrainClient(string name) => services
            .AddHttpClient(name)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All });

        AddBrainClient(nameof(ApiClient));
        AddBrainClient(NotificationService.NotificationsHttpClientName);
    }

    private static void ConfigureJsonOptions(JsonSerializerOptions options)
    {
        // Share the exact same naming policy/null-handling/resolver as the rest of the SDK (see
        // AppJsonSerializerOptions), rather than configuring MVC's options independently.
        options.DictionaryKeyPolicy = options.PropertyNamingPolicy = AppJsonSerializerOptions.Default.PropertyNamingPolicy;
        options.DefaultIgnoreCondition = AppJsonSerializerOptions.Default.DefaultIgnoreCondition;
        options.TypeInfoResolver = AppJsonSerializerOptions.Default.TypeInfoResolver;
    }

    private static void ConfigureLoggingDefaults(HostBuilderContext context, ILoggingBuilder builder)
    {
        builder
            .ClearProviders()
            .SetMinimumLevel(LogLevel.Warning)
            .AddSimpleConsole(options => options.SingleLine = true);
        if (context.HostingEnvironment.IsDevelopment())
        {
            builder.AddDebug();
        }
    }

    private static void ConfigureServices(IServiceCollection services, Brain brain, Func<IServiceProvider, IReadOnlyCollection<IDeviceBuilder>> devices, string adapterName, IServiceConfiguration[]? serviceConfigurations)
    {
        services
            .AddSingleton<IApiClient, ApiClient>()
            .AddSingleton<IBrainRecipes, BrainRecipes>()
            .AddSingleton<IDeviceDatabase, DeviceDatabase>()
            .AddSingleton<IDynamicDeviceRegistry, DynamicDeviceRegistry>()
            .AddSingleton<INotificationMapping, NotificationMapping>()
            .AddSingleton<INotificationService, NotificationService>()
            .AddSingleton<IPgpEncryption, PgpEncryption>()
            .AddSingleton<ISdkEnvironment, SdkEnvironment>()
            .AddSingleton<IBrain>(brain)
            .AddSingleton(devices)
            .AddSingleton((SdkAdapterName)$"src-{UniqueNameGenerator.Generate(adapterName)}")
            .AddHostedService<SdkRegistration>()
            .AddHostedService<SubscriptionsNotifier>()
            .AddHostedService<UriPrefixNotifier>();
        Array.ForEach(serviceConfigurations ?? [], configuration => configuration.ConfigureServices(services));
    }

    private static void ConfigureWebHostDefaults(IWebHostBuilder builder, IPAddress hostAddress, int port) => builder
        .ConfigureKestrel(options =>
        {
            options.AddServerHeader = false;
            options.Limits.MaxRequestBodySize = Constants.MaxRequestBodySize;
            options.Listen(hostAddress, port);
        })
        .ConfigureServices(services =>
        {
            services
               .AddMvcCore(options => options.AllowEmptyInputInBodyModelBinding = true)
               .AddJsonOptions(options => Server.ConfigureJsonOptions(options.JsonSerializerOptions))
               .AddCors(options => options.AddDefaultPolicy(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()))
               .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(AssemblyControllerFeatureProvider.Instance));
        })
        .Configure((context, builder) =>
        {
            builder
                .UseRouting()
                .UseCors()
                .UseEndpoints(endpoints => endpoints.MapControllers());
        });

    private static class Constants
    {
        public const int MaxRequestBodySize = 2 * 1024 * 1024;
    }

    /// <summary>
    /// Registers all controllers in this assembly, whether public or internal.
    /// </summary>
    private sealed class AssemblyControllerFeatureProvider : ControllerFeatureProvider
    {
        public static readonly ControllerFeatureProvider Instance = new AssemblyControllerFeatureProvider();

        protected override bool IsController(TypeInfo info)
        {
            return info.Assembly == Assembly.GetExecutingAssembly() && info.IsAssignableTo(typeof(ControllerBase));
        }
    }
}
