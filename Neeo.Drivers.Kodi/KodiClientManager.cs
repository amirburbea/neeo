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

[Service]
public sealed class KodiClientManager(KodiClientFactory clientFactory, ILogger<KodiClientManager> logger) : IDisposable
{
    private readonly ConcurrentDictionary<string, KodiClient> _clients = new();
    private PeriodicTimer? _discoveryTimer;
    private Task? _initializationTask;

    public event EventHandler<DataEventArgs<KodiClient>>? ClientDiscovered;

    public IEnumerable<KodiClient> Clients => this._clients.Values;

    public async Task DiscoverAsync(int scanTime = 5000, Func<KodiClient, bool>? completionTest = default, CancellationToken cancellationToken = default)
    {
        CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            await ZeroconfResolver.ResolveAsync(
                Constants.HttpServiceName,
                TimeSpan.FromMilliseconds(scanTime),
                callback: OnHostDiscovered,
                cancellationToken: cts.Token
            );
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return;
        }

        async void OnHostDiscovered(IZeroconfHost host)
        {
            IPAddress ipAddress = IPAddress.Parse(host.IPAddress);
            if (this._clients.Values.Any(client => client.IPAddress.Equals(ipAddress)))
            {
                // IP address previously discovered.
                return;
            }
            logger.LogInformation("Found client ({name}) at IP address {ip}.", host.DisplayName, ipAddress);
            KodiClient client = clientFactory.CreateClient(host.DisplayName, ipAddress, host.Services.First().Value.Port);
            if (!await client.ConnectAsync(cancellationToken).ConfigureAwait(false) || client.MacAddress.Equals(PhysicalAddress.None))
            {
                logger.LogWarning("Something went wrong, ignoring client at {ip}.", ipAddress);
                client.Dispose();
                return;
            }
            this.OnClientDiscovered(client);
            if (completionTest != null && completionTest.Invoke(client))
            {
                cts.Cancel();
            }
        }
    }

    public void Dispose()
    {
        Interlocked.Exchange(ref this._discoveryTimer, default)?.Dispose();
        foreach (KodiClient client in this._clients.Values)
        {
            client.Dispose();
        }
    }

    public KodiClient? GetClientOrDefault(string id) => this._clients.GetValueOrDefault(id);

    public Task InitializeAsync(string? deviceId = default, CancellationToken cancellationToken = default)
    {
        return this._initializationTask ??= InitializeDiscoveryAsync();

        async Task InitializeDiscoveryAsync()
        {
            try
            {
                // Use a short initial window, consider initialization complete if a device is discovered.
                await this.DiscoverAsync(2000, client => (deviceId ?? client.DeviceId) == client.DeviceId, cancellationToken).ConfigureAwait(false);
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

    private void OnClientDiscovered(KodiClient client)
    {
        this._clients[client.DeviceId] = client;
        this.ClientDiscovered?.Invoke(this, client);
    }

    private static class Constants
    {
        public const string HttpServiceName = "_xbmc-jsonrpc-h._tcp.local.";
    }
}
