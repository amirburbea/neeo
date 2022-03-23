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
    private Task? _initializationTask;
    private PeriodicTimer? _discoveryTimer;

    public KodiClientManager(ILogger<KodiClientManager> logger) => this._logger = logger;

    public event EventHandler<DataEventArgs<KodiClient>>? ClientDiscovered;

    public IEnumerable<KodiClient> Clients => this._clients.Values;

    public Task DiscoverAsync(int scanTime = 5000, CancellationToken cancellationToken = default)
    {
        return ZeroconfResolver.ResolveAsync(Constants.HttpServiceName, TimeSpan.FromMilliseconds(scanTime), callback: OnHostDiscovered, cancellationToken: cancellationToken);

        async void OnHostDiscovered(IZeroconfHost host)
        {
            IPAddress ipAddress = IPAddress.Parse(host.IPAddress);
            if (this.Clients.Any(client => client.IPAddress.Equals(ipAddress)))
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
        }
    }

    public void Dispose() => Interlocked.Exchange(ref this._discoveryTimer, default)?.Dispose();

    public KodiClient? GetClientOrDefault(string id) => this._clients.GetValueOrDefault(id);

    public Task InitializeAsync()
    {
        return this._initializationTask ??= InitializeDiscoveryAsync();

        async Task InitializeDiscoveryAsync()
        {
            // Use a short initial window.
            await this.DiscoverAsync(2000).ConfigureAwait(false);
            _ = CreateDiscoveryTimer();
        }

        async Task CreateDiscoveryTimer()
        {
            this._discoveryTimer = new PeriodicTimer(TimeSpan.FromMinutes(1d));
            try
            {
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
    }

    private static class Constants
    {
        public const string HttpServiceName = "_xbmc-jsonrpc-h._tcp.local.";
    }
}