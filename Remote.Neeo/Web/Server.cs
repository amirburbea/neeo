using System;
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
using Remote.Neeo.Devices;

namespace Remote.Neeo.Web
{
    internal static class Server
    {
        private static IHost? _host;

        public static async Task StartAsync(Brain brain, string name, IDeviceBuilder[] devices, IPAddress ipAddress, ushort port, CancellationToken cancellationToken)
        {
            if (Server._host != null)
            {
                throw new InvalidOperationException("Host is already running.");
            }
            if (brain == null)
            {
                throw new ArgumentNullException(nameof(brain));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Non-blank name is required.", nameof(name));
            }
            if (devices == null || devices.Length == 0 || Array.IndexOf(devices, default) != -1)
            {
                throw new ArgumentException("Devices collection can not be null/empty or contain null.", nameof(devices));
            }
            string adapterName = $"src-{UniqueNameGenerator.Generate(name)}";
            IHost host = Server.CreateHostBuilder(brain, adapterName, devices, ipAddress, port).Build();
            await host.StartAsync(cancellationToken).ConfigureAwait(false);
            Server._host = host;
            ILogger<Brain> logger = host.Services.GetRequiredService<ILogger<Brain>>();
            string baseUrl = $"http://{ipAddress}:{port}";
            for (int i = 0; i < Constants.MaxConnectionRetries; i++)
            {
                try
                {
                    await brain.PostAsync("api/registerSdkDeviceAdapter", new { Name = adapterName, BaseUrl = baseUrl }, cancellationToken).ConfigureAwait(false);
                    logger.LogInformation("SDK Adapter registered on brain @ http://{host}:{port}", brain.HostName, brain.Port);
                    return;
                }
                catch
                {
                    logger.LogWarning("Failed to register with brain {times} time(s).", i + 1);
                }
            }
            throw new ApplicationException("Failed to connect to brain.");
        }

        public static async Task StopAsync(CancellationToken cancellationToken)
        {
            using IHost? host = Interlocked.Exchange(ref Server._host, null);
            if (host == null)
            {
                return;
            }
            ILogger<Brain> logger = host.Services.GetRequiredService<ILogger<Brain>>();
            Brain brain = host.Services.GetRequiredService<Brain>();
            string adapterName = host.Services.GetRequiredService<SdkAdapterName>().Name;
            await brain.PostAsync("api/unregisterSdkDeviceAdapter", new { Name = adapterName }, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("SDK Adapter unregistered from brain @ http://{host}:{port}", brain.HostName, brain.Port);
            await host.StopAsync(cancellationToken).ConfigureAwait(false);
        }

        private static IHostBuilder CreateHostBuilder(Brain brain, string adapterName, IDeviceBuilder[] devices, IPAddress ipAddress, ushort port) => Host.CreateDefaultBuilder().ConfigureWebHostDefaults(builder =>
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
                    builder.ClearProviders().AddConsole();
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        builder.AddDebug();
                    }
                })
                .ConfigureServices((context, services) =>
                {
                    services
                        .AddSingleton(brain)
                        .AddSingleton(new DeviceDatabase(Array.ConvertAll(devices, devices => devices.BuildAdapter())))
                        .AddSingleton(new SdkAdapterName(adapterName))
                        .AddCors(options => options.AddPolicy(nameof(CorsPolicy), builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()))
                        .AddSingleton<PgpKeys>()
                        .AddControllers()
                        .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new AllowInternalsControllerFeatureProvider()));
                })
                .Configure((context, builder) =>
                {
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        builder.UseDeveloperExceptionPage();
                    }
                    builder
                        .UseRouting()
                        .UseMiddleware<PgpMiddleware>()
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
