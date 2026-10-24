using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.WebOS;

[SettingsStore("weboskeys.json.bin")]
public sealed class WebOSClientManager(
    ILogger<WebOSClient> clientLogger,
    ILogger<WebOSClientManager> logger,
    ISettingsStore<WebOSClientManager> settings
) : IDisposable
{
    // Settings keys: "<deviceId>" holds the pairing (client) key, "<deviceId>|ip" the last known IP address (so a TV that is
    // off - and therefore missing from the ARP table - at startup can still be found).
    private const string IPAddressSuffix = "|ip";
    private static readonly int[] _ports = [3001, 3000];

    private readonly ConcurrentDictionary<string, WebOSClient> _clients = new(StringComparer.Ordinal);
    private bool _isDisposed;

    public event EventHandler<DataEventArgs<WebOSClient>>? ClientAdded;

    public IReadOnlyCollection<WebOSClient> Clients => [.. this._clients.Values];

    /// <summary>
    /// Gets a value indicating whether any TV has been paired (even if it is not currently known or connected).
    /// </summary>
    public bool HasPairedDevices => settings.Keys.Any(static key => !key.EndsWith(IPAddressSuffix, StringComparison.Ordinal));

    public WebOSClient? GetClient(string deviceId) => this._clients.GetValueOrDefault(deviceId);

    public bool TryGetConnected(string deviceId, [NotNullWhen(true)] out WebOSClient? client)
    {
        client = this.GetClient(deviceId) is { IsConnected: true } connected ? connected : null;
        return client is not null;
    }

    public bool TryRemove(string deviceId, [NotNullWhen(true)] out WebOSClient? client) => this._clients.TryRemove(deviceId, out client);

    /// <summary>
    /// Gets the client for the TV, creating it (and, if it is paired, starting to connect it in the background) if needed.
    /// </summary>
    public WebOSClient GetOrCreateClient(IPAddress ipAddress, PhysicalAddress macAddress, int port = 3000)
    {
        ObjectDisposedException.ThrowIf(this._isDisposed, this);
        string deviceId = macAddress.Equals(PhysicalAddress.None) ? ipAddress.ToString() : macAddress.ToString();
        if (this._clients.GetValueOrDefault(deviceId) is { } existing)
        {
            return existing;
        }
        WebOSClient client = this.CreateClient(deviceId, ipAddress, macAddress, port);
        if (!this._clients.TryAdd(deviceId, client))
        {
            // Another caller registered the same TV in the meantime - keep that one.
            client.Dispose();
            return this._clients[deviceId];
        }
        settings[deviceId + IPAddressSuffix] = ipAddress.ToString();
        this.ClientAdded?.Invoke(this, client);
        client.RequestReconnect();
        return client;
    }

    /// <summary>
    /// Gets the client for a device the Brain already knows, looking up its IP address in the ARP table and falling back to the
    /// last known address.
    /// </summary>
    public WebOSClient? GetOrCreateKnownClient(string deviceId)
    {
        if (this.GetClient(deviceId) is { } existing)
        {
            return existing;
        }
        if (!PhysicalAddress.TryParse(deviceId, out PhysicalAddress? macAddress))
        {
            return IPAddress.TryParse(deviceId, out IPAddress? address) ? this.GetOrCreateClient(address, PhysicalAddress.None) : null;
        }
        IPAddress? ipAddress = NetworkMethods.GetNetworkDevices().FirstOrDefault(device => device.Value.Equals(macAddress)).Key;
        if (ipAddress is null && settings.Get(deviceId + IPAddressSuffix) is { } text && IPAddress.TryParse(text, out IPAddress? stored))
        {
            ipAddress = stored;
        }
        return ipAddress is null ? null : this.GetOrCreateClient(ipAddress, macAddress);
    }

    /// <summary>
    /// Finds webOS TVs on the network (anything answering on the webOS ports), creating clients for them. Does not pair.
    /// </summary>
    public async Task<WebOSClient[]> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        ConcurrentBag<WebOSClient> discovered = [];
        KeyValuePair<IPAddress, PhysicalAddress>[] networkDevices = [.. NetworkMethods.GetNetworkDevices()];

        await Task.WhenAll(
            networkDevices.Select(async device =>
            {
                try
                {
                    (IPAddress ip, PhysicalAddress mac) = device;
                    foreach (int port in _ports)
                    {
                        if (await WebOSClientManager.ProbePortAsync(ip, port, 400, cancellationToken).ConfigureAwait(false))
                        {
                            discovered.Add(this.GetOrCreateClient(ip, mac));
                            return;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ignore cancellation
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Failed to probe device {ip}", device.Key);
                }
            })
        ).ConfigureAwait(false);

        return [.. discovered];
    }

    /// <summary>
    /// Discovers TVs and pairs each one that is not paired yet, one at a time (each shows a prompt on the TV and waits up to 60
    /// seconds for it to be accepted).
    /// </summary>
    /// <returns><see langword="true"/> if at least one TV is paired once done.</returns>
    public async Task<bool> PairAsync(CancellationToken cancellationToken = default)
    {
        foreach (WebOSClient client in await this.DiscoverAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!client.IsPaired)
            {
                await client.PairAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        return this.HasPairedDevices;
    }

    private WebOSClient CreateClient(string deviceId, IPAddress ipAddress, PhysicalAddress macAddress, int port)
    {
        WebOSClient client = new(ipAddress, macAddress, clientLogger, settings.Get(deviceId), port);
        client.Registered += (_, e) =>
        {
            logger.LogInformation("Saving webOS client key for device {deviceId}.", client.DeviceId);
            settings[client.DeviceId] = e.Data;
        };
        return client;
    }

    private static async Task<bool> ProbePortAsync(IPAddress ip, int port, int timeoutMs, CancellationToken cancellationToken)
    {
        try
        {
            using TcpClient tcpClient = new();
            using CancellationTokenSource timeoutCts = new(timeoutMs);
            using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            await tcpClient.ConnectAsync(ip, port, linkedCts.Token).ConfigureAwait(false);
            return tcpClient.Connected;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (this._isDisposed)
        {
            return;
        }
        this._isDisposed = true;
        foreach (WebOSClient client in this._clients.Values)
        {
            client.Dispose();
        }
    }
}
