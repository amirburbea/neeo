using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
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
using Remote.Neeo.Devices;

namespace Remote.Neeo.Web
{
    internal static class Server
    {
        public static async Task<IHost> StartAsync(Brain brain, string name, IDeviceBuilder[] devices, IPAddress hostIPAddress, int port, CancellationToken cancellationToken)
        {
            string adapterName = $"src-{UniqueNameGenerator.Generate(name)}";
            IHost host = Server.CreateHostBuilder(
                brain ?? throw new ArgumentNullException(nameof(brain)),
                adapterName,
                devices ?? throw new ArgumentNullException(nameof(devices)),
                hostIPAddress ?? throw new ArgumentNullException(nameof(hostIPAddress)),
                port
            ).Build();
            await host.StartAsync(cancellationToken).ConfigureAwait(false);
            ILogger<Brain> logger = host.Services.GetRequiredService<ILogger<Brain>>();
            for (int i = 0; i < Constants.MaxConnectionRetries; i++)
            {
                try
                {
                    await brain.RegisterServerAsync(adapterName, $"http://{hostIPAddress}:{port}", cancellationToken).ConfigureAwait(false);
                    logger.LogInformation("Server registered on {brain} (http://{hostIPAddress}:{port}).", brain.HostName, hostIPAddress, port);
                    return host;
                }
                catch (Exception e)
                {
                    logger.LogWarning("Failed to register with brain {times} time(s).\n{content}", i + 1, e.Message);
                }
            }
            throw new ApplicationException("Failed to connect to brain.");
        }

        public static async Task StopAsync(IHost? host, CancellationToken cancellationToken)
        {
            if (host == null)
            {
                return;
            }
            try
            {
                ILogger<Brain> logger = host.Services.GetRequiredService<ILogger<Brain>>();
                Brain brain = host.Services.GetRequiredService<Brain>();
                string name = host.Services.GetRequiredService<SdkAdapterName>().Name;
                await brain.UnregisterServerAsync(name, cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Server unregistered from {brain}.", brain.HostName);
                await host.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                host.Dispose();
            }
        }

        private static IHostBuilder CreateHostBuilder(Brain brain, string name, IDeviceBuilder[] devices, IPAddress ipAddress, int port) => Host.CreateDefaultBuilder().ConfigureWebHostDefaults(builder =>
        {
            builder
                .ConfigureKestrel((context, options) =>
                {
                    options.Limits.MaxRequestBodySize = Constants.MaxRequestBodySize;
                    options.Listen(ipAddress, port);
                    if (context.HostingEnvironment.IsDevelopment() && !ipAddress.Equals(IPAddress.Loopback))
                    {
                        options.Listen(IPAddress.Loopback, port);
                    }
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
                        .AddSingleton(brain)
                        .AddSingleton<IReadOnlyCollection<IDeviceAdapter>>(Array.ConvertAll(devices, devices => devices.BuildAdapter()))
                        .AddSingleton<IDeviceDatabase, DeviceDatabase>()
                        .AddSingleton(new SdkAdapterName(name))
                        .AddSingleton<PgpKeys>()
                        .AddCors(options => options.AddPolicy(nameof(CorsPolicy), builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()))
                        .AddControllers(options => options.AllowEmptyInputInBodyModelBinding = true)
                        .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new AllowInternalsControllerFeatureProvider()))
                        .AddJsonOptions(options =>
                        {
                            options.JsonSerializerOptions.IgnoreNullValues = true;
                            options.JsonSerializerOptions.DictionaryKeyPolicy = options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                        });
                })
                .Configure((context, builder) =>
                {
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        builder.UseDeveloperExceptionPage();
                    }
                    builder
                        .UseMiddleware<PgpMiddleware>()
                        .UseRouting()
                        .UseCors(nameof(CorsPolicy))
                        .UseEndpoints(endpoints => endpoints.MapControllers());
                });
        });

        private static class Constants
        {
            public const int MaxConnectionRetries = 8;

            public const int MaxRequestBodySize = 2 * 1024 * 1024; // 2mb
        }

        private sealed class AllowInternalsControllerFeatureProvider : ControllerFeatureProvider
        {
            protected override bool IsController(TypeInfo info) => info.Assembly == typeof(Server).Assembly && info.IsAssignableTo(typeof(ControllerBase));
        }

        private sealed record SdkAdapterName
        {
            public SdkAdapterName(string name) => this.Name = name;

            public string Name { get; }
        }
    }
}
