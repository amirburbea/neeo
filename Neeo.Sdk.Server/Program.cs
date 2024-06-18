using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Server;

public static class Program
{
    public static Task Main() => new HostBuilder()
        .ConfigureHostConfiguration(Program.ConfigureHostConfiguration)
        .ConfigureAppConfiguration((context, builder) => Program.ConfigureAppConfiguration(context.HostingEnvironment, builder))
        .ConfigureServices((context, services) => Program.ConfigureServices(context.Configuration, services))
        .ConfigureLogging((context, builder) => Program.ConfigureLogging(context.HostingEnvironment, builder))
        .RunConsoleAsync();

    private static void ConfigureAppConfiguration(IHostEnvironment environment, IConfigurationBuilder builder) => builder
        .AddEnvironmentVariables(prefix: "NEEO_")
        .AddCommandLine(Environment.GetCommandLineArgs()[1..])
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true);

    private static void ConfigureHostConfiguration(IConfigurationBuilder builder) => builder
        .AddEnvironmentVariables(prefix: "DOTNET_")
        .AddCommandLine(Environment.GetCommandLineArgs()[1..]);

    private static void ConfigureLogging(IHostEnvironment environment, ILoggingBuilder builder)
    {
        builder
            .ClearProviders()
            .AddSimpleConsole(options => options.SingleLine = true);
        if (!environment.IsProduction())
        {
            builder.AddDebug();
        }
    }

    private static void ConfigureServices(IConfiguration configuration, IServiceCollection services)
    {
        if (configuration.GetSection("Drivers").Get<string[]>() is not { Length: > 0 } driverPaths)
        {
            throw new ApplicationException("Invalid configuration, configuration should have an array \"Drivers\" with at least one driver assembly.");
        }
        foreach (string driverPath in driverPaths)
        {
            string assemblyPath = Path.GetFullPath(driverPath);
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException(assemblyPath);
            }
            foreach (Type type in new DriverAssemblyLoadContext(assemblyPath).Assembly.GetExportedTypes())
            {
                if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
                {
                    continue;
                }
                if (typeof(IDeviceProvider).IsAssignableFrom(type))
                {
                    services.Add(new(typeof(IDeviceProvider), type, ServiceLifetime.Singleton));
                }
                else if (typeof(IServiceConfiguration).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) is { } constructor)
                {
                    ((IServiceConfiguration)constructor.Invoke(null)).ConfigureServices(services);
                }
            }
            services
                .Configure<HostOptions>(options => options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost)
                .AddHostedService<SdkService>();
        }
    }

    private sealed class DriverAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public DriverAssemblyLoadContext(string assemblyPath)
        {
            this._resolver = new(assemblyPath);
            this.Assembly = this.LoadFromAssemblyName(AssemblyName.GetAssemblyName(assemblyPath));
        }

        public Assembly Assembly { get; }

        protected override Assembly? Load(AssemblyName assemblyName) => this._resolver.ResolveAssemblyToPath(assemblyName) is { } path
            ? this.LoadFromAssemblyPath(path)
            : default;

        protected override nint LoadUnmanagedDll(string unmanagedDllName) => this._resolver.ResolveUnmanagedDllToPath(unmanagedDllName) is { } path
            ? this.LoadUnmanagedDllFromPath(path)
            : default;
    }
}
