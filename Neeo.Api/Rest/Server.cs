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

namespace Neeo.Api.Rest;

/// <summary>
/// Contains <see langword="static"/> methods for starting and stopping a REST server for interacting with the NEEO
/// Brain.
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
        await host.Services.GetRequiredService<ServerRegistration>().RegisterAsync(cancellationToken).ConfigureAwait(false);
        await host.Services.GetRequiredService<SubscriptionsNotifier>().NotifySubscriptionsAsync(cancellationToken).ConfigureAwait(false);
        return host;
    }

    public static async Task StopAsync(IHost host, CancellationToken cancellationToken = default)
    {
        using (host)
        {
            await host.Services.GetRequiredService<ServerRegistration>().UnregisterAsync(cancellationToken).ConfigureAwait(false);
            await host.StopAsync(cancellationToken).ConfigureAwait(false);
        }
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
                        .AddSingleton<ServerRegistration>()
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
                        .AddSingleton<IDynamicDeviceRegistrar, DynamicDevices>();

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

    private sealed class ServerRegistration
    {
        private readonly IApiClient _client;
        private readonly SdkEnvironment _environment;
        private readonly ILogger<ServerRegistration> _logger;

        public ServerRegistration(
            SdkEnvironment environment,
            IApiClient client,
            ILogger<ServerRegistration> logger
        ) => (this._environment, this._client, this._logger) = (environment, client, logger);

        public Task RegisterAsync(CancellationToken cancellationToken = default)
        {
            return RegisterAsync(Constants.MaxConnectionRetries);

            async Task RegisterAsync(int retryCount)
            {
                try
                {
                    if (await this._client.RegisterServerAsync(this._environment.SdkAdapterName, $"http://{this._environment.HostEndPoint}", cancellationToken).ConfigureAwait(false))
                    {
                        this._logger.LogInformation(
                            "Server {adapterName} registered on {brainHost} ({brainIP}).",
                            this._environment.SdkAdapterName,
                            this._environment.BrainHostName,
                            this._environment.BrainEndPoint.Address
                        );
                    }
                }
                catch (Exception) when (retryCount > 0)
                {
                    this._logger.LogWarning("Failed to register with brain. Will retry up to {times} more time(s).", retryCount);
                    await RegisterAsync(retryCount - 1).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    this._logger.LogError(e, "Failed to register on the brain - giving up.");
                    throw new ApplicationException("Failed to register with brain.", e);
                }
            }
        }

        public async Task UnregisterAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (await this._client.UnregisterServerAsync(this._environment.SdkAdapterName, cancellationToken).ConfigureAwait(false))
                {
                    this._logger.LogInformation("Server unregistered from {brain}.", this._environment.BrainHostName);
                }
            }
            catch (Exception e)
            {
                this._logger.LogWarning("Failed to unregister with brain\n{content}", e.Message);
            }
        }
    }
}