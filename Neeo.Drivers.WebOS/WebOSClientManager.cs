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
    private readonly DeviceRegistry<WebOSClient> _clients = new(logger);
    private readonly ConcurrentDictionary<string, Task<WebOSClient?>> _connectingClients = new(StringComparer.Ordinal);
    private bool _isDisposed;

    public IReadOnlyCollection<WebOSClient> Clients => this._clients.Clients;

    public IDeviceRegistry<WebOSClient> Registry => this._clients;

    public WebOSClient? GetClient(string deviceId) => this._clients.GetClient(deviceId);

    public bool TryGetConnected(string deviceId, [NotNullWhen(true)] out WebOSClient? client)
    {
        return this._clients.TryGetConnected(deviceId, out client);
    }

    public async Task<WebOSClient?> GetOrCreateClientAsync(
        IPAddress ipAddress,
        PhysicalAddress macAddress,
        int port = 3000,
        CancellationToken cancellationToken = default
    )
    {
        string deviceId = macAddress.Equals(PhysicalAddress.None) ? ipAddress.ToString() : macAddress.ToString();
        if (this._clients.GetClient(deviceId) is { } existing)
        {
            return existing;
        }
        return await this._connectingClients.GetOrAdd(
            deviceId,
            async deviceId =>
            {
                string? clientKey = settings.TryGetValue(deviceId, out string? key) ? key : null;
                WebOSClient client = new(ipAddress, macAddress, clientLogger, clientKey, port);

                client.Registered += (_, e) =>
                {
                    logger.LogInformation("Saving webOS client key for device {deviceId}.", client.DeviceId);
                    settings[client.DeviceId] = e.Data;
                };

                this._clients.TryAdd(client);
                await client.ConnectAsync(cancellationToken).ConfigureAwait(false);
                return client;
            }
        ).ConfigureAwait(false);
    }

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
                    if (await WebOSClientManager.ProbePortAsync(ip, 3000, 400, cancellationToken).ConfigureAwait(false))
                    {
                        if (await this.GetOrCreateClientAsync(ip, mac, 3000, cancellationToken).ConfigureAwait(false) is { } client)
                        {
                            discovered.Add(client);
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
        foreach (WebOSClient client in this._clients.Clients)
        {
            client.Dispose();
        }
    }
}
