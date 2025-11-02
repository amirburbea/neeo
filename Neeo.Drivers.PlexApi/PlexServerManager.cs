using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Neeo.Drivers.PlexApi;

public interface IPlexServerManager
{
    Task<IPlexServer?> GetServerAsync(string name, CancellationToken cancellationToken = default);
}

internal partial class PlexServerManager(
    IPlexServerDiscovery discovery,
    IPlexTokenStore tokenStore,
    IHttpClientFactory httpClientFactory,
    IPlexSettingsManager settingsManager,
    ILogger<PlexServerManager> logger
) : IPlexServerManager, IDisposable
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Plex");
    private readonly ConcurrentDictionary<string, (PlexServer, List<PlexServerConsumer>)> _servers = [];

    public void Dispose()
    {
        // Dispose removes items, so a .ToList() is mandatory.
        Parallel.ForEach(
            this._servers.Values.SelectMany(tuple => tuple is (_, { } consumers) ? consumers : []).ToList(),
            consumer => consumer.Dispose()
        );
        this._httpClient.Dispose();
    }

    public async Task<IPlexServer?> GetServerAsync(string name, CancellationToken cancellationToken)
    {
        if (!discovery.Servers.ContainsKey(name))
        {
            return null;
        }
        PlexServerConsumer consumer;
        if (this._servers.GetValueOrDefault(name) is ({ } server, { } consumers))
        {
            consumers.Add(consumer = new(server));
        }
        else
        {
            server = new(name, discovery, tokenStore, this._httpClient, settingsManager, logger);
            this._servers[name] = (server, [consumer = new(server)]);
            await server.InitializeAsync(cancellationToken).ConfigureAwait(false);
        }
        consumer.Disposed
           .Take(1)
           .Subscribe((_) => this.OnPlexConsumerDisposed(consumer));
        return consumer;
    }

    private void OnPlexConsumerDisposed(PlexServerConsumer consumer)
    {
        if (this._servers.GetValueOrDefault(consumer.Name) is not ({ } server, { } consumers))
        {
            return;
        }
        if (consumers.Remove(consumer) && consumers.Count == 0)
        {
            // After removing the last consumer, dispose of the connection.
            server.Dispose();
            this._servers.TryRemove(consumer.Name, out _);
        }
    }
}
