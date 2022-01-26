using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
using Neeo.Api.Devices;
using Neeo.Api.Devices.Controllers;
using Neeo.Api.Json;
using Neeo.Api.Notifications;
using Neeo.Api.Utilities;

namespace Neeo.Api.Rest;

/// <summary>
/// Contains <see langword="static"/> methods for starting and stopping a REST server
/// for interacting with the NEEO Brain.
/// </summary>
internal static class Server
{
    public static async Task<IHost> StartAsync(
        Brain brain,
        string name,
        IReadOnlyCollection<IDeviceBuilder> devices,
        IPAddress? hostIPAddress,
        int port,
        CancellationToken cancellationToken = default
    )
    {
        string sdkAdapterName = $"src-{UniqueNameGenerator.Generate(name)}";
        IHost host = Server.CreateHostBuilder(
            brain ?? throw new ArgumentNullException(nameof(brain)),
            sdkAdapterName,
            new(hostIPAddress ?? await Server.GetFallbackHostIPAddress(brain.IPAddress, cancellationToken).ConfigureAwait(false), port),
            devices ?? throw new ArgumentNullException(nameof(devices))
        ).Build();
        await host.StartAsync(cancellationToken).ConfigureAwait(false);
        await Server.RegisterAsync(
            host.Services.GetRequiredService<IApiClient>(),
            host.Services.GetRequiredService<ILogger<Brain>>(),
            host.Services.GetRequiredService<SdkEnvironment>(),
            cancellationToken
        ).ConfigureAwait(false);
        await host.Services.GetRequiredService<SubscriptionsNotifier>().NotifySubscriptionsAsync(cancellationToken).ConfigureAwait(false);
        return host;
    }

    public static async Task StopAsync(IHost host, CancellationToken cancellationToken = default)
    {
        using IDisposable _ = host;
        await Task.WhenAll(
            host.StopAsync(cancellationToken),
            Server.UnregisterAsync(
                host.Services.GetRequiredService<IApiClient>(),
                host.Services.GetRequiredService<ILogger<Brain>>(),
                host.Services.GetRequiredService<SdkEnvironment>(),
                cancellationToken
            )
        ).ConfigureAwait(false);
    }

    private static IHostBuilder CreateHostBuilder(Brain brain, string sdkAdapterName, IPEndPoint hostEndPoint, IReadOnlyCollection<IDeviceBuilder> devices)
    {
        return Host.CreateDefaultBuilder().ConfigureWebHostDefaults(builder =>
        {
            builder
                .ConfigureKestrel((context, options) =>
                {
                    options.Limits.MaxRequestBodySize = Constants.MaxRequestBodySize;
                    options.Listen(hostEndPoint);
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder
                        .ClearProviders()
                        .AddConsole();
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        builder.AddDebug();
                    }
                })
                .ConfigureServices((context, services) =>
                {
                    // Controller configuration.
                    services
                        .AddMvcCore(options => options.AllowEmptyInputInBodyModelBinding = true)
                        .AddCors(options => options.AddPolicy(nameof(CorsPolicy), builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()))
                        .AddJsonOptions(options => options.JsonSerializerOptions.UpdateConfiguration())
                        .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(AllowInternalsControllerFeatureProvider.Instance));
                    // Server startup tasks.
                    services
                        .AddSingleton<SubscriptionsNotifier>();
                    // Dependencies.
                    services
                        .AddSingleton(devices)
                        .AddSingleton(new SdkEnvironment(sdkAdapterName, hostEndPoint, new(brain.IPAddress, brain.ServicePort), brain.HostName))
                        .AddSingleton<PgpKeys>()
                        .AddSingleton<DiscoveryControllerFactory>()
                        .AddSingleton<IApiClient, ApiClient>()
                        .AddSingleton<IDeviceDatabase, DeviceDatabase>()
                        .AddSingleton<INotificationMapping, NotificationMapping>()
                        .AddSingleton<INotificationService, NotificationService>()
                        .AddSingleton<IDynamicDevices, DynamicDevices>()
                        .AddSingleton<IDynamicDeviceRegistrar, DynamicDevices>()
                        .AddSingleton<DiscoveryControllerFactory>();
                })
                .Configure((context, builder) =>
                {
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        builder.UseDeveloperExceptionPage();
                    }
                    builder
                        .UseRouting()
                        .UseCors(nameof(CorsPolicy))
                        .UseEndpoints(endpoints => endpoints.MapControllers());
                });
        });
    }

    private static async ValueTask<IPAddress> GetFallbackHostIPAddress(IPAddress brainIPAddress, CancellationToken cancellationToken)
    {
        if (IPAddress.IsLoopback(brainIPAddress))
        {
            return brainIPAddress;
        }
        IPAddress[] addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName(), AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);
        return Array.IndexOf(addresses, brainIPAddress) == -1 && Array.Find(addresses, address => !IPAddress.IsLoopback(address)) is { } address
            ? address
            : IPAddress.Loopback;
    }

    private static async Task RegisterAsync(IApiClient client, ILogger logger, SdkEnvironment environment, CancellationToken cancellationToken = default)
    {
        for (int attempt = 0; attempt <= Constants.MaxConnectionRetries; attempt++)
        {
            try
            {
                bool success = await client.PostAsync(
                    UrlPaths.RegisterServer,
                    new { Name = environment.AdapterName, BaseUrl = $"http://{environment.HostEndPoint}" },
                    static (SuccessResult result) => result.Success,
                    cancellationToken
                ).ConfigureAwait(false);
                if (!success)
                {
                    throw new ApplicationException("Failed to register on the brain - registration rejected.");
                }
                logger.LogInformation(
                    "Server {adapterName} registered on {brainHost} ({brainIP}).",
                    environment.AdapterName,
                    environment.BrainHostName,
                    environment.BrainEndPoint.Address
                );
                break;
            }
            catch (Exception) when (attempt < Constants.MaxConnectionRetries)
            {
                logger.LogWarning("Failed to register with brain (on attempt #{attempt}). Retrying...", attempt + 1);
                continue;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to register on the brain - giving up.");
                throw;
            }
        }
    }

    private static async Task UnregisterAsync(IApiClient client, ILogger logger, SdkEnvironment environment, CancellationToken cancellationToken = default)
    {
        try
        {
            bool success = await client.PostAsync(
                UrlPaths.UnregisterServer,
                new { Name = environment.AdapterName },
                static (SuccessResult result) => result.Success,
                cancellationToken
            ).ConfigureAwait(false);
            if (success)
            {
                logger.LogInformation("Server unregistered from {brain}.", environment.BrainHostName);
            }
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed to unregister with brain - {content}.", e.Message);
        }
    }

    private static class Constants
    {
        public const int MaxConnectionRetries = 8;

        public const int MaxRequestBodySize = 2 * 1024 * 1024;
    }

    private sealed class AllowInternalsControllerFeatureProvider : ControllerFeatureProvider
    {
        public static readonly ControllerFeatureProvider Instance = new AllowInternalsControllerFeatureProvider();

        protected override bool IsController(TypeInfo info) => info.Assembly == this.GetType().Assembly && info.IsAssignableTo(typeof(ControllerBase));
    }
}