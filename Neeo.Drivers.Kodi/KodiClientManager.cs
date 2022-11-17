using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Utilities;
using Zeroconf;

namespace Neeo.Drivers.Kodi;

public sealed class KodiClientManager : IDisposable
{
    private readonly ConcurrentDictionary<string, KodiClient> _clients = new();
    private readonly ILogger<KodiClientManager> _logger;
    private PeriodicTimer? _discoveryTimer;
    private Task? _initializationTask;

    public KodiClientManager(ILogger<KodiClientManager> logger) => this._logger = logger;

    public event EventHandler<DataEventArgs<KodiClient>>? ClientDiscovered;

    public IEnumerable<KodiClient> Clients => this._clients.Values;

    public Task DiscoverAsync(int scanTime = 5000, CancellationToken cancellationToken = default) => this.DiscoverAsync(scanTime, default, cancellationToken);

    public Task DiscoverAsync(int scanTime, Func<KodiClient, bool>? considerDiscoveryComplete, CancellationToken cancellationToken = default)
    {
        CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        return ZeroconfResolver.ResolveAsync(Constants.HttpServiceName, TimeSpan.FromMilliseconds(scanTime), callback: OnHostDiscovered, cancellationToken: source.Token);

        async void OnHostDiscovered(IZeroconfHost host)
        {
            IPAddress ipAddress = IPAddress.Parse(host.IPAddress);
            if (this.Clients.Any(ipAddress.Equals))
            {
                return;
            }
            this._logger.LogInformation("Found client ({name}) at IP address {ip}.", host.DisplayName, host.IPAddress);
            KodiClient client = new(host.DisplayName, ipAddress, host.Services.First().Value.Port);
            if (!await client.ConnectAsync(cancellationToken).ConfigureAwait(false) || client.MacAddress.Equals(PhysicalAddress.None))
            {
                this._logger.LogWarning("Something went wrong, ignoring client at {ip}.", client.IPAddress);
                client.Dispose();
                return;
            }
            this._clients[client.MacAddress.ToString()] = client;
            this.ClientDiscovered?.Invoke(this, client);
            if (considerDiscoveryComplete != null && considerDiscoveryComplete(client))
            {
                source.Cancel();
            }
        }
    }

    public void Dispose() => Interlocked.Exchange(ref this._discoveryTimer, default)?.Dispose();

    public KodiClient? GetClientOrDefault(string id) => this._clients.GetValueOrDefault(id);

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        return this._initializationTask ??= InitializeDiscoveryAsync();

        async Task InitializeDiscoveryAsync()
        {
            try
            {
                // Use a short initial window.
                await this.DiscoverAsync(2000, static _ => true, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Ignore.
            }
            finally
            {
                _ = this.CreateDiscoveryTimer();
            }
        }
    }

    private async Task CreateDiscoveryTimer()
    {
        try
        {
            this._discoveryTimer = new(TimeSpan.FromMinutes(1d));
            while (await this._discoveryTimer.WaitForNextTickAsync().ConfigureAwait(false))
            {
                await this.DiscoverAsync().ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // This is expected when the class gets disposed.
        }
    }

    private static class Constants
    {
        public const string HttpServiceName = "_xbmc-jsonrpc-h._tcp.local.";
    }
}