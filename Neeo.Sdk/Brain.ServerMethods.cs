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
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Controllers;
using Neeo.Sdk.Json;
using Neeo.Sdk.Notifications;
using Neeo.Sdk.Rest;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk;

public partial class Brain
{
    private IHost? _host;

    /// <summary>
    /// Asynchronously starts the SDK integration server and registers it on the NEEO Brain.
    /// </summary>
    /// <param name="name">A name for your integration server. This name should be consistent upon restarting the driver host server.</param>
    /// <param name="devices">An array of devices to register with the NEEO Brain.</param>
    /// <param name="hostIPAddress">
    /// The IP Address on which to bind the integration server. If not specified, falls back to the first non-loopack IPv4 address or <see cref="IPAddress.Loopback"/> if not found.
    /// <para />
    /// Note: If in development, the server also listens on localhost at the specified <paramref name="port"/>.
    /// </param>
    /// <param name="port">The port to listen on.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    public async Task StartServerAsync(IReadOnlyCollection<IDeviceBuilder> devices, string? name = default, IPAddress? hostIPAddress = null, ushort port = 9000, CancellationToken cancellationToken = default)
    {
        if (devices is not { Count: > 0 })
        {
            throw new ArgumentException("At least one device is required.", nameof(devices));
        }
        if (port == default)
        {
            throw new ArgumentException("Port can not be 0.", nameof(port));
        }
        if (this._host != null)
        {
            throw new InvalidOperationException("Server is already running.");
        }
        SdkEnvironment environment = new(
            $"src-{UniqueNameGenerator.Generate(name ?? Dns.GetHostName())}",
            this.ServiceEndPoint,
            this.HostName,
            new(hostIPAddress ?? await this.GetFallbackHostIPAddress(cancellationToken).ConfigureAwait(false), port)
        );
        IHost host = Brain.CreateHostBuilder(environment, devices).Build();
        await host.StartAsync(cancellationToken).ConfigureAwait(false);
        this._host = host;
    }

    private static IHostBuilder CreateHostBuilder(SdkEnvironment environment, IReadOnlyCollection<IDeviceBuilder> devices) => Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(builder => ConfigureWebHostDefaults(builder, environment, devices))
        // Add startup tasks which needs to be run on startup but only after the web host has started.
        .ConfigureServices(services => services.AddHostedService<ServerRegistration>().AddHostedService<SubscriptionsNotifier>());

    private static void ConfigureWebHostDefaults(
        IWebHostBuilder builder,
        SdkEnvironment environment,
        IReadOnlyCollection<IDeviceBuilder> devices
    ) => builder
            .ConfigureKestrel((context, options) =>
            {
                options.Limits.MaxRequestBodySize = Constants.MaxRequestBodySize;
                options.Listen(environment.HostEndPoint);
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
                // Controller configuration.
                services
                    .AddMvcCore(options => options.AllowEmptyInputInBodyModelBinding = true)
                    .AddCors(options => options.AddPolicy(nameof(CorsPolicy), builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()))
                    .AddJsonOptions(options => options.JsonSerializerOptions.UpdateConfiguration())
                    .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(AssemblyControllerFeatureProvider.Instance));


                // Dependencies.
                services
                    .AddSingleton<ISdkEnvironment>(environment)
                    .AddSingleton(devices)
                    .AddSingleton(() => PgpKeys.Create())
                    .AddSingleton<DiscoveryControllerFactory>()
                    .AddSingleton<IApiClient, ApiClient>()
                    .AddSingleton<IDeviceDatabase, DeviceDatabase>()
                    .AddSingleton<INotificationMapping, NotificationMapping>()
                    .AddSingleton<INotificationService, NotificationService>()
                    .AddSingleton<IDynamicDevices, DynamicDevices>()
                    .AddSingleton<IDynamicDeviceRegistrar, DynamicDevices>()
                    .AddSingleton<DiscoveryControllerFactory>();
            })
            .Configure((context, appBuilder) =>
            {
                if (context.HostingEnvironment.IsDevelopment())
                {
                    appBuilder.UseDeveloperExceptionPage();
                }

                appBuilder
                    .UseRouting()
                    .UseCors(nameof(CorsPolicy))
                    .UseEndpoints(endpoints => endpoints.MapControllers());
            });

    /// <summary>
    /// Asynchronously stops the SDK integration server and unregisters it from the Brain.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns><see cref="Task"/> to indicate completion.</returns>
    public async Task StopServerAsync(CancellationToken cancellationToken = default)
    {
        using IHost? host = Interlocked.Exchange(ref this._host, null);
        if (host != null)
        {
            await host.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask<IPAddress> GetFallbackHostIPAddress(CancellationToken cancellationToken)
    {
        if (IPAddress.IsLoopback(this.IPAddress))
        {
            return this.IPAddress;
        }
        IPAddress[] addresses = await Dns.GetHostAddressesAsync(Dns.GetHostName(), AddressFamily.InterNetwork, cancellationToken).ConfigureAwait(false);
        return Array.IndexOf(addresses, this.IPAddress) == -1 && Array.Find(addresses, address => !IPAddress.IsLoopback(address)) is { } address
            ? address
            : IPAddress.Loopback;
    }

    private record struct SdkEnvironment(string AdapterName, IPEndPoint BrainEndPoint, string BrainHostName, IPEndPoint HostEndPoint) : ISdkEnvironment;

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

public interface ISdkEnvironment
{
    string AdapterName { get; }

    IPEndPoint BrainEndPoint { get; }

    IPEndPoint HostEndPoint { get; }

    void Deconstruct(out string adapterName, out IPEndPoint brainEndPoint, out string brainHostName, out IPEndPoint hostEndPoint);
}