using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remote.Neeo.Devices;

namespace Remote.Neeo.Server
{
    public static class HostManager
    {
        private static IHost? _host;

        public static async Task StartAsync(Brain brain, IEnumerable<IDeviceBuilder> devices, int port = 3201)
        {
            if (HostManager._host != null)
            {
                throw new InvalidOperationException("Host is already running - it must be stopped to start a new host.");
            }
            IHost host = HostManager._host = Host.CreateDefaultBuilder().ConfigureWebHostDefaults(builder => builder
                .ConfigureKestrel((context, options) =>
                {
                    options.Limits.MaxRequestBodySize = 2 * 1024 * 1024; // 2mb
                    options.ListenAnyIP(port);
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
                        .AddSingleton<PgpKeys>()
                        .AddSingleton(new DeviceDatabase(devices))
                        .AddCors(options => options.AddPolicy(nameof(CorsPolicy), builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()))
                        .AddControllers();
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
                })).Build();
            await host.RunAsync().ConfigureAwait(false);
        }

        public static Task StopAsync() => Interlocked.Exchange(ref HostManager._host, null)?.StopAsync() ?? Task.CompletedTask;
    }
}
