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
using Neeo.Api.Json;
using Neeo.Api.Notifications;

namespace Neeo.Api.Rest;

/// <summary>
/// Contains <see langword="static"/> methods for starting and stopping a REST server for interacting with the NEEO
/// Brain.
/// </summary>
internal static class Server
{
    public static async Task<IHost> StartAsync(Brain brain, string name, IReadOnlyCollection<IDeviceBuilder> devices, IPAddress? hostIPAddress, int port, CancellationToken cancellationToken)
    {
        string sdkAdapterName = $"src-{UniqueNameGenerator.Generate(name)}";
        IHost host = Server.CreateHostBuilder(
            brain ?? throw new ArgumentNullException(nameof(brain)),
            sdkAdapterName,
            new(hostIPAddress ??= await Server.GetFallbackHostIPAddress(brain.IPAddress, cancellationToken).ConfigureAwait(false), port),
            devices ?? throw new ArgumentNullException(nameof(devices))
        ).Build();
        await host.StartAsync(cancellationToken).ConfigureAwait(false);
        ILogger<Brain> logger = host.Services.GetRequiredService<ILogger<Brain>>();
        IApiClient client = host.Services.GetRequiredService<IApiClient>();
        for (int i = 0; i < Constants.MaxConnectionRetries; i++)
        {
            try
            {
                if (await client.RegisterServerAsync(sdkAdapterName, $"http://{hostIPAddress}:{port}", cancellationToken).ConfigureAwait(false))
                {
                    logger.LogInformation("Server {adapterName} registered on {brainHost} ({brainIP}).", sdkAdapterName, brain.HostName, brain.IPAddress);
                    return host;
                }
            }
            catch (Exception e)
            {
                logger.LogWarning("Failed to register with brain {times} time(s).\n{content}", i, e.Message);
            }
        }
        throw new ApplicationException("Failed to register with brain.");
    }

    public static async Task StopAsync(IHost host, CancellationToken cancellationToken)
    {
        using (host)
        {
            ILogger<Brain> logger = host.Services.GetRequiredService<ILogger<Brain>>();
            IApiClient client = host.Services.GetRequiredService<IApiClient>();
            ISdkEnvironment environment = host.Services.GetRequiredService<ISdkEnvironment>();
            try
            {
                if (await client.UnregisterServerAsync(environment.SdkAdapterName, cancellationToken).ConfigureAwait(false))
                {
                    logger.LogInformation("Server unregistered from {brain}.", environment.BrainHostName);
                }
            }
            catch (Exception e)
            {
                logger.LogWarning("Failed to unregister with brain\n{content}", e.Message);
            }
            await host.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static IHostBuilder CreateHostBuilder(Brain brain, string sdkAdapterName, IPEndPoint hostEndPoint, IReadOnlyCollection<IDeviceBuilder> devices) => Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(builder =>
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
                    services
                        .AddSingleton(devices)
                        .AddSingleton<IApiClient, ApiClient>()
                        .AddSingleton<IDeviceCompiler, DeviceCompiler>()
                        .AddSingleton<IDeviceDatabase, DeviceDatabase>()
                        .AddSingleton<IDeviceSubscriptions, DeviceSubscriptions>()
                        .AddSingleton<INotificationMapping, NotificationMapping>()
                        .AddSingleton<INotificationService, NotificationService>()
                        .AddSingleton<ISdkEnvironment>(new SdkEnvironment(
                            sdkAdapterName, 
                            new(brain.IPAddress, brain.ServicePort),
                            brain.HostName,
                            hostEndPoint
                        ));
                    services
                        .AddMvcCore(options => options.AllowEmptyInputInBodyModelBinding = true)
                        .AddCors(options => options.AddPolicy(nameof(CorsPolicy), builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()))
                        .AddJsonOptions(options => options.JsonSerializerOptions.UpdateConfiguration())
                        .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(AllowInternalsControllerFeatureProvider.Instance));
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

    private static async ValueTask<IPAddress> GetFallbackHostIPAddress(IPAddress brainIPAddress, CancellationToken cancellationToken)
    {
        if (brainIPAddress != IPAddress.Loopback)
        {
            IPAddress[] addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName(), AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);
            if (Array.IndexOf(addresses, brainIPAddress) == -1 && Array.Find(addresses, address => !IPAddress.IsLoopback(address)) is { } address)
            {
                return address;
            }
        }
        return IPAddress.Loopback;
    }

    private static Task<bool> RegisterServerAsync(this IApiClient client, string name, string baseUrl, CancellationToken cancellationToken) => client.PostAsync(
        UrlPaths.RegisterServer,
        new { Name = name, BaseUrl = baseUrl },
        static (SuccessResult result) => result.Success,
        cancellationToken
    );

    private static Task<bool> UnregisterServerAsync(this IApiClient client, string name, CancellationToken cancellationToken) => client.PostAsync(
        UrlPaths.UnregisterServer,
        new { Name = name },
        static (SuccessResult result) => result.Success,
        cancellationToken
    );

    private sealed record class SdkEnvironment(
        string SdkAdapterName, 
        IPEndPoint BrainEndPoint,
        string BrainHostName,
        IPEndPoint HostEndPoint
    ) : ISdkEnvironment;

    private static class Constants
    {
        public const int MaxConnectionRetries = 8;

        public const int MaxRequestBodySize = 2 * 1024 * 1024;
    }

    private sealed class AllowInternalsControllerFeatureProvider : ControllerFeatureProvider
    {
        public static readonly ControllerFeatureProvider Instance = new AllowInternalsControllerFeatureProvider();

        private AllowInternalsControllerFeatureProvider()
        {
        }

        protected override bool IsController(TypeInfo info) => info.Assembly == this.GetType().Assembly && info.IsAssignableTo(typeof(ControllerBase));
    }
}