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
    public static async Task<IHost> StartAsync(Brain brain, string name, IDeviceBuilder[] devices, IPAddress? hostIPAddress, int port, CancellationToken cancellationToken)
    {
        string adapterName = $"src-{UniqueNameGenerator.Generate(name)}";
        IHost host = Server.CreateHostBuilder(
            brain ?? throw new ArgumentNullException(nameof(brain)),
            adapterName,
            devices ?? throw new ArgumentNullException(nameof(devices)),
            hostIPAddress ??= await Server.GetFallbackHostIPAddress(brain.IPAddress, cancellationToken),
            port
        ).Build();
        await host.StartAsync(cancellationToken).ConfigureAwait(false);
        ILogger<Brain> logger = host.Services.GetRequiredService<ILogger<Brain>>();
        IApiClient client = host.Services.GetRequiredService<IApiClient>();
        for (int i = 0; i < Constants.MaxConnectionRetries; i++)
        {
            try
            {
                if (await client.RegisterServerAsync(adapterName, $"http://{hostIPAddress}:{port}", cancellationToken).ConfigureAwait(false))
                {
                    SdkEnvironment environment = host.Services.GetRequiredService<SdkEnvironment>();
                    logger.LogInformation("Server http://{hostIP}:{port} ({adapterName}) registered on {brainHost}.local ({brainIP}).", hostIPAddress, port, environment.SdkAdapterName, brain.HostName, brain.IPAddress);
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
            Brain brain = host.Services.GetRequiredService<Brain>();
            IApiClient client = host.Services.GetRequiredService<IApiClient>();
            string name = host.Services.GetRequiredService<SdkEnvironment>().SdkAdapterName;
            try
            {
                if (await client.UnregisterServerAsync(name, cancellationToken).ConfigureAwait(false))
                {
                    logger.LogInformation("Server unregistered from {brain}.", brain.HostName);
                }
            }
            catch (Exception e)
            {
                logger.LogWarning("Failed to unregister with brain\n{content}", e.Message);
            }
            await host.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static IHostBuilder CreateHostBuilder(Brain brain, string sdkAdapterName, IDeviceBuilder[] devices, IPAddress ipAddress, int port) => Host.CreateDefaultBuilder().ConfigureWebHostDefaults(builder =>
      {
          builder
              .ConfigureKestrel((context, options) =>
              {
                  options.Limits.MaxRequestBodySize = Constants.MaxRequestBodySize;
                  options.Listen(ipAddress, port);
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
                      .AddSingleton<IApiClient, ApiClient>()
                      .AddSingleton<IDeviceDatabase, DeviceDatabase>()
                      .AddSingleton<INotificationService, NotificationService>()
                      .AddSingleton((IReadOnlyCollection<IDeviceAdapter>)Array.ConvertAll(devices, device => device.BuildAdapter()))
                      .AddSingleton(new SdkEnvironment(sdkAdapterName))
                      .AddSingleton(brain)
                      .AddCors(options => options.AddPolicy(nameof(CorsPolicy), builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()))
                      .AddControllers(options => options.AllowEmptyInputInBodyModelBinding = true)
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

                      //.UseMiddleware<PgpMiddleware>()
                      .UseRouting()
                      .UseCors(nameof(CorsPolicy))
                      .UseEndpoints(endpoints => endpoints.MapControllers());
              });
      });

    private static async Task<IPAddress> GetFallbackHostIPAddress(IPAddress brainIPAddress, CancellationToken cancellationToken)
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
        (SuccessResult result) => result.Success,
        cancellationToken
    );

    private static Task<bool> UnregisterServerAsync(this IApiClient client, string name, CancellationToken cancellationToken) => client.PostAsync(
        UrlPaths.UnregisterServer,
        new { Name = name },
        (SuccessResult result) => result.Success,
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
        { }

        protected override bool IsController(TypeInfo info) => info.Assembly == this.GetType().Assembly && info.IsAssignableTo(typeof(ControllerBase));
    }
}