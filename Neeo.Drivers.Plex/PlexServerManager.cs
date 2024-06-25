using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;

namespace Neeo.Drivers.Plex;

public interface IPlexServerManager
{
    IPlexServer GetServer(IPAddress ipAddress);
}

internal sealed class PlexServerManager(
    IHttpClientFactory httpClientFactory,
    IPlexDriverSettings driverSettings,
    ILogger<PlexServer> logger
) : IPlexServerManager
{
    private readonly string? _dnsSuffix = Array.Find(
        NetworkInterface.GetAllNetworkInterfaces(),
        adapter => adapter.OperationalStatus is OperationalStatus.Up && adapter.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211
    )?.GetIPProperties().DnsSuffix;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(nameof(Plex));
    private readonly ConcurrentDictionary<IPAddress, PlexServer> _servers = [];

    public PlexServer GetServer(IPAddress ipAddress) => this._servers.GetOrAdd(ipAddress, (address) =>
    {
        PlexServer server = new(address, this._dnsSuffix, this._httpClient, driverSettings, logger);
        server.Destroyed += this.Server_Destroyed;
        return server;
    });

    IPlexServer IPlexServerManager.GetServer(IPAddress ipAddress) => this.GetServer(ipAddress);

    private void Server_Destroyed(object? sender, EventArgs e)
    {
        PlexServer server = (PlexServer)sender!;
        server.Destroyed -= this.Server_Destroyed;
        this._servers.TryRemove(server.IPAddress, out _);
    }
}
