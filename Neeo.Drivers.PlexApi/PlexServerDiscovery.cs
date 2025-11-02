using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Neeo.Drivers.PlexApi;

public interface IPlexServerDiscovery
{
    IReadOnlyDictionary<string, PlexServerInfo> Servers { get; }

    Task InitializeAsync();
}

internal sealed class PlexServerDiscovery : IPlexServerDiscovery, IDisposable
{
    private static readonly byte[] _requestBytes = Encoding.ASCII.GetBytes("M-SEARCH * HTTP/1.0\r\n\r\n");

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ConcurrentDictionary<string, PlexServerInfo> _discoveredServers = [];
    private readonly Lazy<Task> _discoveryTask;
    private readonly TaskCompletionSource _initialDiscoverySource = new();
    private readonly ILogger _logger;

    public PlexServerDiscovery(ILogger<PlexServerDiscovery> logger)
    {
        this._discoveryTask = new(() => Task.Run(this.DiscoverServers, this.CancellationToken), true);
        this._logger = logger;
    }

    IReadOnlyDictionary<string, PlexServerInfo> IPlexServerDiscovery.Servers => this._discoveredServers;

    private CancellationToken CancellationToken => this._cancellationTokenSource.Token;

    public void Dispose()
    {
        using CancellationTokenSource cts = this._cancellationTokenSource;
        cts.Cancel();
        if (this._discoveryTask.IsValueCreated)
        {
            this._discoveryTask.Value.Wait();
        }
    }

    public async Task InitializeAsync()
    {
        if (this._discoveryTask.Value.Status is not TaskStatus.Canceled or TaskStatus.Faulted)
        {
            await this._initialDiscoverySource.Task.ConfigureAwait(false);
        }
    }

    private async Task DiscoverServers()
    {
        this._logger.LogInformation("Starting Plex server discovery...");
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
            this._logger.LogError(ex, "Error during Plex server discovery.");
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
        cts.CancelAfter(TimeSpan.FromSeconds(1.5d));
        using UdpClient udpClient = new() { Client = { EnableBroadcast = true } };
        await udpClient.SendAsync(PlexServerDiscovery._requestBytes, new(IPAddress.Broadcast, Constants.DiscoveryPort), cts.Token).ConfigureAwait(false);
        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync(cts.Token).ConfigureAwait(false);
                string response = Encoding.UTF8.GetString(result.Buffer);
                if (!response.StartsWith("HTTP/1.0 200 OK"))
                {
                    continue;
                }
                string? name = null;
                using (StringReader reader = new(response))
                {
                    while (reader.ReadLine() is { } line)
                    {
                        if (line.StartsWith(Constants.NamePrefix))
                        {
                            name = line[Constants.NamePrefix.Length..].Trim();
                            break;
                        }
                    }
                }
                if (name != null)
                {
                    this.ProcessDiscoveredServer(new(name, result.RemoteEndPoint.Address));
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
                this._logger.LogError(ex, "Error receiving data");
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
                this._logger.LogInformation("Discovered Plex server '{Name}' ({IPAddress})", server.Name, server.IPAddress);
                return server;
            },
            (_, existing) =>
            {
                if (!existing.Equals(server))
                {
                    this._logger.LogInformation("Plex server '{Name}' ({IPAddress}) updated", server.Name, server.IPAddress);
                }
                return server;
            }
        );
    }

    private static class Constants
    {
        public const int DiscoveryPort = 32414;
        public const string NamePrefix = "Name:";
        public const string ResourceIdentifier = "Resource-Identifier:";
    }
}
