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

namespace Neeo.Drivers.Plex;

public interface IPlexServerDiscovery
{
    IReadOnlyDictionary<string, PlexServerData> Servers { get; }

    Task InitializeAsync();
}

internal sealed class PlexServerDiscovery : IPlexServerDiscovery, IDisposable
{
    private static readonly byte[] _requestBytes = Encoding.ASCII.GetBytes("M-SEARCH * HTTP/1.0\r\n\r\n");

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ConcurrentDictionary<string, PlexServerData> _discoveredServers = [];
    private readonly Lazy<Task> _discoveryTask;
    private readonly TaskCompletionSource _initializationSource = new();
    private readonly ILogger _logger;

    public PlexServerDiscovery(ILogger<PlexServerDiscovery> logger)
    {
        this._discoveryTask = new(() => Task.Run(this.DiscoverServersAsync, this.CancellationToken), true);
        this._logger = logger;
    }

    IReadOnlyDictionary<string, PlexServerData> IPlexServerDiscovery.Servers => this._discoveredServers;

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

    public Task InitializeAsync()
    {
        _ = this._discoveryTask.Value; // Ensure task has been started.
        return this._initializationSource.Task;
    }

    private async Task DiscoverServersAsync()
    {
        this._logger.LogInformation("Starting Plex server discovery...");
        try
        {
            using PeriodicTimer timer = new(TimeSpan.FromMinutes(5));
            do
            {
                try
                {
                    await DiscoverAsync().ConfigureAwait(false);
                }
                finally
                {
                    this._initializationSource.TrySetResult();
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

        async Task DiscoverAsync()
        {
            using CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(this.CancellationToken);
            source.CancelAfter(TimeSpan.FromSeconds(1.5d));
            using UdpClient udpClient = new() { Client = { EnableBroadcast = true } };
            await udpClient.SendAsync(PlexServerDiscovery._requestBytes, new(IPAddress.Broadcast, Constants.DiscoveryPort), source.Token).ConfigureAwait(false);
            while (!source.Token.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult result = await udpClient.ReceiveAsync(source.Token).ConfigureAwait(false);
                    string response = Encoding.UTF8.GetString(result.Buffer);
                    if (!response.StartsWith("HTTP/1.0 200 OK"))
                    {
                        continue;
                    }
                    string name = string.Empty;
                    string machineIdentifier = string.Empty;
                    using StringReader reader = new(response);
                    while ((name, machineIdentifier) is not ({ Length: > 0 }, { Length: > 0 }) && reader.ReadLine() is { } line)
                    {
                        if (line.StartsWith(Constants.NamePrefix))
                        {
                            name = line[(Constants.NamePrefix.Length + 1)..];
                        }
                        else if (line.StartsWith(Constants.ResourceIdentifier))
                        {
                            machineIdentifier = line[(Constants.ResourceIdentifier.Length + 1)..];
                        }
                    }
                    if ((name, machineIdentifier) is ({ Length: > 0 }, { Length: > 0 }))
                    {
                        IPAddress ipAddress = result.RemoteEndPoint.Address;
                        this._discoveredServers.AddOrUpdate(
                            name,
                            (name) =>
                            {
                                this._logger.LogInformation("Discovered Plex server '{Name}' ({IPAddress})", name, ipAddress);
                                return new(name, machineIdentifier, ipAddress);
                            },
                            (name, existing) =>
                            {
                                if (existing.IPAddress.Equals(ipAddress))
                                {
                                    return existing;
                                }
                                this._logger.LogInformation("Plex server '{Name}' ({IPAddress}) updated", name, ipAddress);
                                return new(name, machineIdentifier, ipAddress);
                            }
                        );
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
    }

    private static class Constants
    {
        public const int DiscoveryPort = 32414;
        public const string NamePrefix = "Name:";
        public const string ResourceIdentifier = "Resource-Identifier:";
    }
}
