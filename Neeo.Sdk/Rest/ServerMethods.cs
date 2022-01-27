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
using Neeo.Sdk.Utilities;
/*
namespace Neeo.Sdk.Rest;

/// <summary>
/// Contains <see langword="static"/> methods for starting and stopping a REST server
/// for interacting with the NEEO Brain.
/// </summary>
internal static class ServerMethods
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
        if (devices is not { Count: > 0 })
        {
        }
        string sdkAdapterName = $"src-{UniqueNameGenerator.Generate(name)}";
        if (hostIPAddress == null)
        {
            hostIPAddress = await ServerMethods.GetFallbackHostIPAddress(brain.IPAddress, cancellationToken).ConfigureAwait(false);
        }
        IHost host = ServerMethods.CreateHostBuilder(brain, sdkAdapterName, new(hostIPAddress, port), devices).Build();
        await host.StartAsync(cancellationToken).ConfigureAwait(false);
        return host;
    }

    

    private static IHostBuilder CreateHostBuilder(
        Brain brain,
        string sdkAdapterName,
        IPEndPoint hostEndPoint,
        IReadOnlyCollection<IDeviceBuilder> devices
    ) => Host.CreateDefaultBuilder()
        .ConfigureWebHostDefaults(builder => ServerMethods.ConfigureWebHostDefaults(builder, hostEndPoint, brain, sdkAdapterName, devices))
        // Add startup tasks which needs to be run on startup but only after the web host has started.
        .ConfigureServices(services => services.AddHostedService<ServerRegistration>().AddHostedService<SubscriptionsNotifier>());

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
*/