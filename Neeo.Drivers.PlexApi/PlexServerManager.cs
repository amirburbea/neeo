using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Neeo.Drivers.PlexApi;

public interface IPlexServerManager
{
    Task<IPlexServer?> GetServerAsync(string serverName, CancellationToken cancellationToken = default);
}

internal partial class PlexServerManager(
    IPlexDiscovery discovery,
    IPlexTokenStore tokenStore,
    IHttpClientFactory httpClientFactory,
    ILogger<PlexServerManager> logger
) : IPlexServerManager, IDisposable
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Plex");
    private readonly ConcurrentDictionary<string, (PlexServerConnection, ICollection<PlexServerConsumer>)> _servers = [];

    public void Dispose()
    {
        foreach ((_, ICollection<PlexServerConsumer> consumers) in this._servers.Values)
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
        if (this._servers.TryGetValue(serverName, out (PlexServerConnection Connection, ICollection<PlexServerConsumer> Consumers) tuple))
        {
            tuple.Consumers.Add(consumer = new(tuple.Connection));
        }
        else
        {
            PlexServerConnection connection = new(serverName, discovery, tokenStore, this._httpClient, logger);
            this._servers[serverName] = (connection, [consumer = new(connection)]);
            await connection.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }
        consumer.Disposed
            .Take(1)
            .Subscribe((_) => this.OnPlexConsumerDisposed(consumer));
        return consumer;
    }

    private void OnPlexConsumerDisposed(PlexServerConsumer consumer)
    {
        if (!this._servers.TryGetValue(consumer.ServerName, out (PlexServerConnection Connection, ICollection<PlexServerConsumer> Consumers) tuple) ||
            !tuple.Consumers.Remove(consumer))
        {
            return;
        }
        // Check if there are no remaining consumers.
        if (tuple.Consumers.Count == 0)
        {
            // After removing the last consumer, dispose of the connection.
            tuple.Connection.Dispose();
            this._servers.TryRemove(consumer.ServerName, out _);
        }
    }

    private sealed class PlexServerConsumer(PlexServerConnection connection) : IPlexServer, IDisposable
    {
        private readonly Subject<Unit> _disposed = new();

        public IObservable<Unit> Disposed => this._disposed;

        public PlexServerInfo Info => connection.Info;

        public string ServerName => connection.ServerName;

        public PlexPlayerInfo? SelectedPlayer => connection.SelectedPlayer;

        public IObservable<PlexPlayerInfo> SelectedPlayerChanged => connection.SelectedPlayerChanged;

        public void Dispose()
        {
            this._disposed.OnNext(Unit.Default);
            this._disposed.Dispose();
        }

        public Task<PlexPlayerInfo[]> GetPlayersAsync(CancellationToken cancellationToken)
        {
            return connection.GetPlayersAsync(cancellationToken);
        }

        public Task SelectPlayerAsync(string machineIdentifier, CancellationToken cancellationToken)
        {
            return connection.SelectPlayerAsync(machineIdentifier, cancellationToken);
        }
    }
}
