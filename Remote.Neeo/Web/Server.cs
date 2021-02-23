﻿using System;
using System.Collections.Generic;
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
using Remote.Neeo.Json;

namespace Remote.Neeo.Web
{
    /// <summary>
    /// Contains <see langword="static"/> methods for starting and stopping a Kestrel server for interacting with the NEEO Brain.
    /// </summary>
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
                    logger.LogInformation("Server [http://{hostIP}:{port}] registered on {brainHost}.local ({brainIP}).", hostIPAddress, port, brain.HostName, brain.IPAddress);
                    return host;
                }
                catch (Exception e)
                {
                    logger.LogWarning("Failed to register with brain {times} time(s).\n{content}", i, e.Message);
                }
            }
            throw new ApplicationException("Failed to register with brain.");
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
                try
                {
                    await brain.UnregisterServerAsync(name, cancellationToken).ConfigureAwait(false);
                    logger.LogInformation("Server unregistered from {brain}.", brain.HostName);
                }
                catch (Exception e)
                {
                    logger.LogWarning("Failed to unregister with brain\n{content}", e.Message);
                }
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
                        options.ListenLocalhost(port);
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
                        .AddJsonOptions(options => options.JsonSerializerOptions.ApplyOptions())
                        .ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new AllowInternalsControllerFeatureProvider()));
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

            public const int MaxRequestBodySize = 2 * 1024 * 1024;
        }

        private sealed class AllowInternalsControllerFeatureProvider : ControllerFeatureProvider
        {
            protected override bool IsController(TypeInfo info) => info.Assembly == this.GetType().Assembly && info.IsAssignableTo(typeof(ControllerBase));
        }

        private sealed record SdkAdapterName
        {
            public SdkAdapterName(string name) => this.Name = name;

            public string Name { get; }
        }
    }
}
