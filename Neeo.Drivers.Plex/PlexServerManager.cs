using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Neeo.Drivers.Plex;

public interface IPlexServerManager
{
    IReadOnlyCollection<PlexServerData> ServerData { get; }

    IPlexServer? GetServer(string machineIdentifier);

    Task InitializeAsync();

    PlexServerData? TryGetServerData(string machineIdentifier);
}

internal sealed class PlexServerManager : IPlexServerManager, IDisposable
{
    private static readonly byte[] _requestBytes = Encoding.ASCII.GetBytes("M-SEARCH * HTTP/1.0\r\n\r\n");

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ConcurrentDictionary<string, PlexServerData> _discoveryData = [];
    private readonly Lazy<Task> _discoveryTask;
    private readonly HttpClient _httpClient;
    private readonly TaskCompletionSource _initializationSource = new();
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<PlexServerManager> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Dictionary<string, (PlexServer Server, List<IPlexServer> Proxies)> _servers = [];
    private readonly IPlexTokenStore _tokenStore;
    private readonly IPlexSettingsManager _settingsManager;
    private readonly IPlexPlayerManagerFactory _playerManagerFactory;

    public PlexServerManager(
        IHttpClientFactory httpClientFactory,
        IPlexTokenStore tokenStore,
        IPlexSettingsManager settingsManager,
        IPlexPlayerManagerFactory playerManagerFactory,
        ILoggerFactory loggerFactory
    )
    {
        this._tokenStore = tokenStore;
        this._settingsManager = settingsManager;
        this._playerManagerFactory = playerManagerFactory;
        this._loggerFactory = loggerFactory;
        this._httpClient = httpClientFactory.CreateClient("plex");
        this._logger = loggerFactory.CreateLogger<PlexServerManager>();
        this._discoveryTask = new(() => Task.Run(this.DiscoverServersAsync, this.CancellationToken), true);
    }

    IReadOnlyCollection<PlexServerData> IPlexServerManager.ServerData => new ReadOnlyCollectionWrapper<PlexServerData>(this._discoveryData.Values);

    private CancellationToken CancellationToken => this._cancellationTokenSource.Token;
    private Task InitializationTask => this._initializationSource.Task;

    public void Dispose()
    {
        this._cancellationTokenSource.Cancel(); // Stop discovery.
        this._cancellationTokenSource.Dispose();
        List<IPlexServer> proxies;
        try
        {
            this._lock.EnterReadLock();
            proxies = [.. this._servers.Values.SelectMany(tuple => tuple.Proxies)];
        }
        finally
        {
            this._lock.ExitReadLock();
        }
        proxies.ForEach(proxy => proxy.Dispose());
    }

    public IPlexServer? GetServer(string machineIdentifier)
    {
        if (!this._discoveryData.ContainsKey(machineIdentifier))
        {
            return null;
        }
        IPlexServer proxy;
        try
        {
            this._lock.EnterWriteLock();
            if (this._servers.GetValueOrDefault(machineIdentifier) is ({ } server, { } proxies))
            {
                proxies.Add(proxy = PlexServerProxy.Create(server));
            }
            else
            {
                this._servers[machineIdentifier] = (
                    server = this.CreateServer(machineIdentifier),
                    [proxy = PlexServerProxy.Create(server)]
                );
            }
        }
        catch (Exception e)
        {
            this._logger.LogError("Proxy generation failed: {Error}", e);
            throw;
        }
        finally
        {
            this._lock.ExitWriteLock();
        }
        proxy.Disposed
            .Take(1)
            .Subscribe(_ => OnProxyDisposed());
        return proxy;

        void OnProxyDisposed()
        {
            try
            {
                this._lock.EnterUpgradeableReadLock();
                if (this._servers.GetValueOrDefault(machineIdentifier) is not ({ } server, { } proxies))
                {
                    return;
                }
                try
                {
                    this._lock.EnterWriteLock();
                    if (proxies.Remove(proxy) && proxies.Count == 0)
                    {
                        this._servers.Remove(machineIdentifier);
                        server.Dispose();
                    }
                }
                finally
                {
                    this._lock.ExitWriteLock();
                }
            }
            finally
            {
                this._lock.ExitUpgradeableReadLock();
            }
        }
    }

    public Task InitializeAsync()
    {
        _ = this._discoveryTask.Value; // Ensure task has been started.
        return this.InitializationTask;
    }

    public PlexServerData? TryGetServerData(string machineIdentifier) => this._discoveryData.TryGetValue(machineIdentifier, out PlexServerData data) ? data : null;

    private PlexServer CreateServer(string machineIdentifier) => new(
        machineIdentifier,
        this,
        this._tokenStore,
        this._settingsManager,
        this._playerManagerFactory,
        this._httpClient,
        this._loggerFactory.CreateLogger<PlexServer>()
    );

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
            await udpClient.SendAsync(PlexServerManager._requestBytes, new(IPAddress.Broadcast, Constants.DiscoveryPort), source.Token).ConfigureAwait(false);
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
                    using StringReader reader = new(response);
                    string name = "";
                    string machineIdentifier = "";
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
                                _ = int.TryParse(value, out port);
                                break;
                            case Constants.ResourceIdentifierPrefix:
                                machineIdentifier = value;
                                break;
                            default:
                                continue;
                        }
                        if ((name, machineIdentifier, port) is ({ Length: > 0 }, { Length: > 0 }, not 0))
                        {
                            IPAddress ipAddress = result.RemoteEndPoint.Address;
                            this._discoveryData.AddOrUpdate(
                                machineIdentifier,
                                (id) =>
                                {
                                    this._logger.LogInformation("Discovered Plex server '{Name}' ({IPAddress})", name, ipAddress);
                                    return new(name, id, ipAddress, port);
                                },
                                (id, existing) =>
                                {
                                    if ((name, ipAddress, port) == (existing.Name, existing.IPAddress, existing.Port))
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
            }
        }
    }

    private readonly struct ReadOnlyCollectionWrapper<T>(ICollection<T> collection) : IReadOnlyCollection<T>
    {
        public int Count => collection.Count;

        public IEnumerator<T> GetEnumerator() => collection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => collection.GetEnumerator();
    }

    private static class Constants
    {
        public const int DiscoveryPort = 32414;
        public const string NamePrefix = "Name";
        public const string PortPrefix = "Port";
        public const string ResourceIdentifierPrefix = "Resource-Identifier";
    }
}
