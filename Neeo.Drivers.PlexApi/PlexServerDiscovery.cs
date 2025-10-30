using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Neeo.Drivers.PlexApi;

public interface IPlexDiscovery
{
    IReadOnlyDictionary<string, PlexServerInfo> Servers { get; }

    Task InitializeAsync();
}

public readonly record struct PlexServerInfo(string Name, string HostName, IPAddress IPAddress);

internal sealed class PlexServerDiscovery(ILogger<PlexServerDiscovery> logger) : IPlexDiscovery, IDisposable
{
    private static readonly byte[] _requestBytes = Encoding.ASCII.GetBytes("M-SEARCH * HTTP/1.0\r\n\r\n");

    private readonly ConcurrentDictionary<string, PlexServerInfo> _discoveredServers = [];
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private Task? _discoveryTask;
    private readonly TaskCompletionSource _initialDiscoverySource = new();

    IReadOnlyDictionary<string, PlexServerInfo> IPlexDiscovery.Servers => this._discoveredServers;

    private CancellationToken CancellationToken => this._cancellationTokenSource.Token;

    public void Dispose()
    {
        using CancellationTokenSource cts = this._cancellationTokenSource;
        cts.Cancel();
        this._discoveryTask?.Wait();
    }

    public async Task InitializeAsync()
    {
        if (this._discoveryTask == null)
        {
            lock (this._initialDiscoverySource)
            {
                this._discoveryTask ??= Task.Run(this.DiscoverServers, this.CancellationToken);
            }
        }
        await this._initialDiscoverySource.Task.ConfigureAwait(false);
    }

    private async Task DiscoverServers()
    {
        logger.LogInformation("Starting Plex server discovery...");
        using PeriodicTimer timer = new(TimeSpan.FromMinutes(5));
        try
        {
            do
            {
                try
                {
                    await this.DiscoverServersAsync().ConfigureAwait(false);
                }
                finally
                {
                    this._initialDiscoverySource.TrySetResult();
                }
            }
            while (await timer.WaitForNextTickAsync(this.CancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation.
        }
        catch (ObjectDisposedException)
        {
            // Ignore disposal.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during Plex server discovery.");
        }
        finally
        {
            // Probably unnecessary, but ensure the initial discovery task is completed.
            this._initialDiscoverySource.TrySetResult();
        }
    }

    private async Task DiscoverServersAsync()
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(this.CancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(2d));
        using UdpClient udpClient = new() { Client = { EnableBroadcast = true } };
        await udpClient.SendAsync(PlexServerDiscovery._requestBytes, new(IPAddress.Broadcast, Constants.DiscoveryPort), cts.Token).ConfigureAwait(false);
        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync(cts.Token).ConfigureAwait(false);
                string response = Encoding.UTF8.GetString(result.Buffer);
                if (response.StartsWith("HTTP/1.0 200 OK") && response.Split("\r\n", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(line => line.StartsWith(Constants.NamePrefix)) is { } line)
                {
                    string name = line[Constants.NamePrefix.Length..].Trim();
                    IPAddress ipAddress = result.RemoteEndPoint.Address;
                    IPHostEntry entry = await Dns.GetHostEntryAsync(ipAddress.ToString(), cts.Token).ConfigureAwait(false);
                    this.ProcessDiscoveredServer(new(name, entry.HostName, ipAddress));
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error receiving data");
                break;
            }
        }
    }

    private void ProcessDiscoveredServer(PlexServerInfo server)
    {
        this._discoveredServers.AddOrUpdate(
            server.Name,
            (_) =>
            {
                logger.LogInformation("Discovered Plex server '{Name}' ({HostName})", server.Name, server.HostName);
                return server;
            },
            (_, existing) =>
            {
                if (existing.Equals(server))
                {
                    return existing;
                }
                logger.LogInformation("Plex server '{Name}' ({HostName}) updated", server.Name, server.HostName);
                return server;
            }
        );
    }

    private static class Constants
    {
        public const int DiscoveryPort = 32414;

        public const string NamePrefix = "Name:";
    }
}
