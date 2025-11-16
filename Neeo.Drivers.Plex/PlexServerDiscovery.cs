using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Neeo.Drivers.Plex;

public interface IPlexServerDiscovery
{
    IReadOnlyDictionary<string, ServerData> Data { get; }

    Task InitializeAsync();
}

internal sealed class PlexServerDiscovery : IPlexServerDiscovery, IDisposable
{
    private static readonly byte[] _requestBytes = Encoding.ASCII.GetBytes("M-SEARCH * HTTP/1.0\r\n\r\n");

    private readonly IObservable<Unit> _discovery;
    private readonly ConcurrentDictionary<string, ServerData> _discoveryData = [];
    private readonly Lazy<IDisposable> _discoverySubscription;
    private readonly TaskCompletionSource _initializationSource = new();
    private readonly ILogger<PlexServerDiscovery> _logger;

    public PlexServerDiscovery(ILogger<PlexServerDiscovery> logger)
    {
        this._logger = logger;
        this._discovery = Observable.Interval(TimeSpan.FromMinutes(5d))
             .StartWith(0L) // start immediately.
             .Select(_ => Observable.FromAsync(this.DiscoverServersAsync))
             .Switch();
        this._discoverySubscription = new(SubscribeAsync, true);

        IDisposable SubscribeAsync()
        {
            this._logger.LogInformation("Starting Plex server discovery...");
            return this._discovery.Subscribe();
        }
    }

    IReadOnlyDictionary<string, ServerData> IPlexServerDiscovery.Data => this._discoveryData;

    public void Dispose()
    {
        if (this._discoverySubscription.IsValueCreated)
        {
            this._discoverySubscription.Value.Dispose();
        }
    }

    public Task InitializeAsync()
    {
        _ = this._discoverySubscription.Value; // Ensure initialized.
        return this._initializationSource.Task;
    }

    private async Task DiscoverServersAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            this._initializationSource.TrySetCanceled(cancellationToken);
            return;
        }
        using CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
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
                string ipAddress = result.RemoteEndPoint.Address.ToString();
                using StringReader reader = new(response);
                string name = string.Empty;
                string machineIdentifier = string.Empty;
                int port = 0;
                while (reader.ReadLine() is { } line)
                {
                    int index = line.IndexOf(':');
                    if (index is -1)
                    {
                        continue;
                    }
                    string value = line[(index + 2)..]; // : is always followed first by a space.
                    switch (line[..index])
                    {
                        case Constants.NamePrefix:
                            name = value;
                            break;
                        case Constants.PortPrefix:
                            if (!int.TryParse(value, out port))
                            {
                                continue;
                            }
                            break;
                        case Constants.ResourceIdentifierPrefix:
                            machineIdentifier = value;
                            break;
                        default:
                            continue;
                    }
                    if ((name, machineIdentifier, port) is (not "", not "", not 0))
                    {
                        this._discoveryData.AddOrUpdate(
                            machineIdentifier,
                            (id) =>
                            {
                                this._logger.LogInformation("Discovered Plex server '{Name}' ({IPAddress})", name, ipAddress);
                                return new(name, id, ipAddress, port);
                            },
                            (id, existing) =>
                            {
                                if ((name, port, ipAddress) == (existing.Name, existing.Port, existing.IPAddress))
                                {
                                    return existing;
                                }
                                this._logger.LogInformation("Plex server '{Name}' ({IPAddress}) updated", name, ipAddress);
                                return new(name, id, ipAddress, port);
                            }
                        );
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex) when (ex.SocketErrorCode is SocketError.TimedOut)
            {
                break;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error receiving data");
                break;
            }
            finally
            {
                this._initializationSource.TrySetResult();
            }
        }
    }

    private static class Constants
    {
        public const int DiscoveryPort = 32414;
        public const string NamePrefix = "Name";
        public const string PortPrefix = "Port";
        public const string ResourceIdentifierPrefix = "Resource-Identifier";
    }
}
