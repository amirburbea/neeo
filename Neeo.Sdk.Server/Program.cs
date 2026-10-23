using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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
        .ConfigureAppConfiguration((context, builder) => Program.ConfigureAppConfiguration(context.HostingEnvironment, builder, Environment.GetCommandLineArgs()[1..]))
        .ConfigureServices((context, services) => Program.ConfigureServices(context.Configuration, services))
        .ConfigureLogging((context, builder) => Program.ConfigureLogging(context.HostingEnvironment, builder))
        .RunConsoleAsync();

    /// <summary>
    /// Adds the app configuration sources. Later sources override earlier ones, so command line arguments win over
    /// <c>NEEO_</c> environment variables (e.g. <c>NEEO_Kodi__Host</c> for <c>Kodi:Host</c>), which win over the JSON files.
    /// </summary>
    internal static void ConfigureAppConfiguration(IHostEnvironment environment, IConfigurationBuilder builder, string[] args) => builder
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true)
        .AddEnvironmentVariables(prefix: "NEEO_")
        .AddCommandLine(args);

    /// <summary>
    /// Gets the driver assembly paths from the <c>Drivers</c> configuration, either as a single value of paths separated
    /// by semicolons (convenient for an environment variable, and replacing any array from the JSON files) or as an array.
    /// </summary>
    internal static string[] GetDriverPaths(IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection("Drivers");
        string[]? driverPaths = section.Value is { Length: > 0 } text
            ? text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : section.Get<string[]>();
        return driverPaths is { Length: > 0 }
            ? driverPaths
            : throw new ApplicationException("Invalid configuration, configuration should have an array \"Drivers\" with at least one driver assembly.");
    }

    private static void ConfigureHostConfiguration(IConfigurationBuilder builder) => builder
        .AddEnvironmentVariables(prefix: "DOTNET_")
        .AddCommandLine(Environment.GetCommandLineArgs()[1..]);

    private static void ConfigureLogging(IHostEnvironment environment, ILoggingBuilder builder)
    {
        builder
            .ClearProviders()
            .AddSimpleConsole(options => options.SingleLine = true)
            .AddFilter((_, name, level) => level >= LogLevel.Information && (name is null || !name.StartsWith(typeof(HttpClient).FullName!)));
        if (!environment.IsProduction())
        {
            builder.AddDebug();
        }
    }

    private static void ConfigureServices(IConfiguration configuration, IServiceCollection services)
    {
        string[] driverPaths = Program.GetDriverPaths(configuration);
        List<Type> providerTypes = [];
        List<IServiceConfiguration> serviceConfigurations = [];
        foreach (string assemblyPath in driverPaths.Select(Path.GetFullPath))
        {
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException(assemblyPath);
            }
            DriverAssemblyLoadContext context = new(assemblyPath);
            foreach (Type type in context.LoadFromAssemblyName(AssemblyName.GetAssemblyName(assemblyPath)).GetExportedTypes())
            {
                if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
                {
                    continue;
                }
                if (typeof(IDeviceProvider).IsAssignableFrom(type))
                {
                    providerTypes.Add(type);
                }
                else if (typeof(IServiceConfiguration).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) is { } constructor)
                {
                    serviceConfigurations.Add((IServiceConfiguration)constructor.Invoke(null));
                }
            }
        }
        serviceConfigurations.Add(new ConfigurationServiceConfiguration(configuration));
        // Provider types and driver service configurations are handed to SdkService as data, rather than
        // being registered/applied against this (outer) container - SdkService threads them through to
        // Brain.StartServerAsync's Type[] overload, which applies them inside the same (inner) container
        // that hosts IApiClient/IDeviceDatabase/etc., so drivers and the SDK's own services can see each
        // other via normal constructor injection instead of being split across two disconnected hosts.
        services
            .AddSingleton(providerTypes.ToArray())
            .AddSingleton(serviceConfigurations.ToArray())
            .AddSingleton<IBrainDiscovery, BrainDiscovery>()
            .AddSingleton<ISdkServerStarter, SdkServerStarter>()
            .Configure<HostOptions>(options => options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost)
            .AddHostedService<SdkService>();
    }

    private sealed class DriverAssemblyLoadContext(string assemblyPath) : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver = new(assemblyPath);

        protected override Assembly? Load(AssemblyName assemblyName) => this._resolver.ResolveAssemblyToPath(assemblyName) is { } path
            ? this.LoadFromAssemblyPath(path)
            : default;

        protected override nint LoadUnmanagedDll(string unmanagedDllName) => this._resolver.ResolveUnmanagedDllToPath(unmanagedDllName) is { } path
            ? this.LoadUnmanagedDllFromPath(path)
            : default;
    }

    private sealed class ConfigurationServiceConfiguration(IConfiguration configuration) : IServiceConfiguration
    {
        public void ConfigureServices(IServiceCollection services) => services.AddSingleton(configuration);
    }
}
