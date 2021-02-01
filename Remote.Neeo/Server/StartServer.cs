using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Remote.Neeo.Server
{
    public static class StartServer
    {
        public static void SS()
        {
            CreateHostBuilder().Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(int port = 3201)
        {
            return Host.CreateDefaultBuilder().ConfigureWebHostDefaults(builder => builder
                .ConfigureKestrel(options =>
                {
                    options.Limits.MaxRequestBodySize = 2 * 1024 * 1024; // 2mb
                    options.ListenAnyIP(port);
                })
                .ConfigureServices(services =>
                {
                    services
                        .AddSingleton<PgpKeys>()
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
                }));
        }
    }
}
