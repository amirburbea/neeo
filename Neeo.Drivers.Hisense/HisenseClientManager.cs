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

namespace Neeo.Drivers.Hisense;

[SettingsStore("hisensesettings.json.bin")]
public sealed class HisenseClientManager(
    ILogger<HisenseTV> clientLogger,
    ILogger<HisenseClientManager> logger,
    ISettingsStore<HisenseClientManager> settings
) : IDisposable
{
    private const string IPAddressSuffix = "|ip";
    private const int HisenseMqttPort = 36669;

    private readonly DeviceRegistry<HisenseTV> _clients = new(logger);
    private bool _isDisposed;

    public IReadOnlyCollection<HisenseTV> Clients => this._clients.Clients;

    public IDeviceRegistry<HisenseTV> Registry => this._clients;

    public HisenseTV? GetClient(string deviceId) => this._clients.GetClient(deviceId);

    public bool TryGetConnected(string deviceId, [NotNullWhen(true)] out HisenseTV? client)
    {
        return this._clients.TryGetConnected(deviceId, out client);
    }

    public HisenseTV GetOrCreateClient(IPAddress ipAddress, PhysicalAddress macAddress, bool useCertificates = true, string? clientIdPrefix = default)
    {
        ObjectDisposedException.ThrowIf(this._isDisposed, this);
        string deviceId = macAddress.ToString();
        HisenseTV client = this._clients.GetOrAdd(deviceId, _ => new HisenseTV(ipAddress, macAddress, clientLogger, useCertificates, clientIdPrefix), out bool added);
        if (added)
        {
            settings[deviceId + IPAddressSuffix] = ipAddress.ToString();
            client.RequestReconnect();
        }
        else
        {
            this.RefreshAddress(client, ipAddress);
        }
        return client;
    }

    public HisenseTV? GetOrCreateKnownClient(string deviceId)
    {
        if (this._clients.GetClient(deviceId) is { } existing)
        {
            return existing;
        }
        if (!PhysicalAddress.TryParse(deviceId, out PhysicalAddress? macAddress))
        {
            return null;
        }
        IPAddress? ipAddress = NetworkMethods.GetNetworkDevices().FirstOrDefault(device => device.Value.Equals(macAddress)).Key;
        if (ipAddress is null && settings.Get(deviceId + IPAddressSuffix) is { } text && IPAddress.TryParse(text, out IPAddress? stored))
        {
            ipAddress = stored;
        }
        return ipAddress is null ? null : this.GetOrCreateClient(ipAddress, macAddress);
    }

    public async Task<HisenseTV[]> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        ConcurrentBag<HisenseTV> discovered = [];
        KeyValuePair<IPAddress, PhysicalAddress>[] networkDevices = [.. NetworkMethods.GetNetworkDevices()];

        try
        {
            await Task.WhenAll(
            networkDevices.Select(async device =>
            {
                try
                {
                    (IPAddress ip, PhysicalAddress mac) = device;
                    if (this._clients.GetClient(mac.ToString()) is { } known)
                    {
                        // Already registered: just make sure it points at the address it is currently using.
                        if (await HisenseClientManager.ProbePortAsync(ip, HisenseMqttPort, 400, cancellationToken).ConfigureAwait(false))
                        {
                            this.RefreshAddress(known, ip);
                            discovered.Add(known);
                        }
                    }
                    else if (await HisenseClientManager.ProbePortAsync(ip, HisenseMqttPort, 400, cancellationToken).ConfigureAwait(false) &&
                        await HisenseTV.TryCreate(ip, mac, clientLogger, connectionRequired: true, useCertificates: true, clientIdPrefix: default, cancellationToken).ConfigureAwait(false) is { } tv)
                    {
                        // Not registered until the caller adopts it (see Adopt) - the caller owns it until then.
                        discovered.Add(tv);
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
            cancellationToken.ThrowIfCancellationRequested();
        }
        catch
        {
            // Unadopted clients are owned by this call until returned; don't leak their connections and reconnect loops.
            this.Release(discovered);
            throw;
        }

        return [.. discovered];
    }

    /// <summary>
    /// Registers a client returned by <see cref="DiscoverAsync"/> (persisting its address). If a client for the same
    /// device is already registered, that one is returned and <paramref name="client"/> is disposed.
    /// </summary>
    public HisenseTV Adopt(HisenseTV client)
    {
        ObjectDisposedException.ThrowIf(this._isDisposed, this);
        HisenseTV registered = this._clients.GetOrAdd(client.DeviceId, _ => client, out bool added);
        if (added)
        {
            settings[client.DeviceId + IPAddressSuffix] = client.IPAddress.ToString();
        }
        else if (!ReferenceEquals(registered, client))
        {
            registered.UpdateAddress(client.IPAddress);
            settings[client.DeviceId + IPAddressSuffix] = client.IPAddress.ToString();
            client.Dispose();
        }
        return registered;
    }

    /// <summary>
    /// Disposes discovered clients that were never registered; registered clients are left alone.
    /// </summary>
    public void Release(IEnumerable<HisenseTV> clients)
    {
        foreach (HisenseTV client in clients)
        {
            if (!ReferenceEquals(this._clients.GetClient(client.DeviceId), client))
            {
                client.Dispose();
            }
        }
    }

    private void RefreshAddress(HisenseTV client, IPAddress ipAddress)
    {
        if (client.IPAddress.Equals(ipAddress))
        {
            return;
        }
        client.UpdateAddress(ipAddress);
        settings[client.DeviceId + IPAddressSuffix] = ipAddress.ToString();
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
        foreach (HisenseTV client in this._clients.Clients)
        {
            client.Dispose();
        }
    }
}
