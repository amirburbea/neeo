using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.PlexApi;

public interface IPlexServerManager
{
    Task<IPlexServer?> GetServerAsync(string serverName, CancellationToken cancellationToken = default);
}

public interface IPlexServer : IDisposable
{
    PlexServerInfo Info { get; }

    string ServerName { get; }

    PlexPlayerInfo? SelectedPlayer { get; }

    event EventHandler<DataEventArgs<PlexPlayerInfo>>? SelectedPlayerChanged;

    Task<PlexPlayerInfo[]> GetPlayersAsync(CancellationToken cancellationToken = default);

    Task SelectPlayerAsync(string machineIdentifier, CancellationToken cancellationToken = default);
}

internal partial class PlexServerManager(
    IPlexDiscovery discovery,
    IPlexTokenStore tokenStore,
    IHttpClientFactory httpClientFactory,
    ILogger<PlexServerManager> logger
) : IPlexServerManager, IDisposable
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Plex");
    private readonly ConcurrentDictionary<string, (PlexServerConnection, List<PlexServerConsumer>)> _servers = [];

    public void Dispose()
    {
        foreach ((_, List<PlexServerConsumer> consumers) in this._servers.Values)
        {
            foreach (PlexServerConsumer consumer in consumers.ToList())
            {
                consumer.Dispose();
            }
        }
        this._httpClient.Dispose();
    }

    public async Task<IPlexServer?> GetServerAsync(string serverName, CancellationToken cancellationToken = default)
    {
        if (!discovery.Servers.ContainsKey(serverName))
        {
            return null;
        }
        PlexServerConsumer consumer;
        if (this._servers.TryGetValue(serverName, out (PlexServerConnection Connection, List<PlexServerConsumer> Consumers) tuple))
        {
            tuple.Consumers.Add(consumer = new(tuple.Connection));
        }
        else
        {
            PlexServerConnection connection = new(serverName, discovery, tokenStore, this._httpClient, logger);
            this._servers[serverName] = (connection, [consumer = new(connection)]);
            await connection.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }
        consumer.Disposed += this.OnConsumerDisposed;
        return consumer;
    }

    private void OnConsumerDisposed(object? sender, EventArgs e)
    {
        if (sender is PlexServerConsumer consumer)
        {
            if (this._servers.TryGetValue(consumer.ServerName, out (PlexServerConnection Connection, List<PlexServerConsumer> Consumers) tuple) && tuple.Consumers.Remove(consumer) && tuple.Consumers.Count == 0)
            {
                tuple.Connection.Dispose();
                this._servers.TryRemove(consumer.ServerName, out _);
            }
            consumer.Disposed -= this.OnConsumerDisposed;
        }
    }

    private class PlexServerConsumer : IPlexServer, IDisposable
    {
        private readonly PlexServerConnection _connection;

        public PlexServerConsumer(PlexServerConnection connection)
        {
            this._connection = connection;
            this._connection.SelectedPlayerChanged += this.OnSelectedPlayerChanged;
        }

        public PlexServerInfo Info => this._connection.Info;

        public string ServerName => this._connection.ServerName;

        public PlexPlayerInfo? SelectedPlayer => this._connection.SelectedPlayer;

        public event EventHandler? Disposed;

        public event EventHandler<DataEventArgs<PlexPlayerInfo>>? SelectedPlayerChanged;

        public void Dispose()
        {
            this._connection.SelectedPlayerChanged -= this.OnSelectedPlayerChanged;
            this.SelectedPlayerChanged = null;
            this.Disposed?.Invoke(this, EventArgs.Empty);
        }

        public Task<PlexPlayerInfo[]> GetPlayersAsync(CancellationToken cancellationToken = default) => this._connection.GetPlayersAsync(cancellationToken);

        public Task SelectPlayerAsync(string machineIdentifier, CancellationToken cancellationToken) => this._connection.SelectPlayerAsync(machineIdentifier, cancellationToken);

        private void OnSelectedPlayerChanged(object? sender, DataEventArgs<PlexPlayerInfo> e) => this.SelectedPlayerChanged?.Invoke(this, e);
    }
}


