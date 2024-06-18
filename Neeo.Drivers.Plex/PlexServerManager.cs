using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Neeo.Drivers.Plex;

public interface IPlexServerManager
{
    IPlexServer GetServer(IPAddress ipAddress);
}

internal sealed class PlexServerManager(
    IHttpClientFactory httpClientFactory,
    IClientIdentifier clientIdentifier,
    IFileStore fileStore,
    ILogger<PlexServer> logger
) : IPlexServerManager
{
    private readonly string _clientIdentifier = clientIdentifier.Value;
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(nameof(Plex));
    private readonly Dictionary<IPAddress, PlexServer> _servers = [];

    public PlexServer GetServer(IPAddress ipAddress)
    {
        if (this._servers.TryGetValue(ipAddress, out PlexServer? server))
        {
            return server;
        }
        server = new(ipAddress, this._httpClient, this._clientIdentifier, fileStore, logger);
        this._servers.Add(ipAddress, server);
        server.Destroyed += this.Server_Destroyed;
        return server;
    }

    IPlexServer IPlexServerManager.GetServer(IPAddress ipAddress) => this.GetServer(ipAddress);

    private void Server_Destroyed(object? sender, EventArgs e)
    {
        PlexServer server = (PlexServer)sender!;
        server.Destroyed -= this.Server_Destroyed;
        this._servers.Remove(server.IPAddress);
    }
}
