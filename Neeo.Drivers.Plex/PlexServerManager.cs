using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Neeo.Drivers.Plex;

public interface IPlexServerManager
{
    IPlexServer? GetServer(string name);
}

internal partial class PlexServerManager(
  IPlexServerDiscovery discovery,
  IPlexTokenStore tokenStore,
  IHttpClientFactory httpClientFactory,
  IPlexSettingsManager settingsManager,
  ILoggerFactory loggerFactory
) : IPlexServerManager, IDisposable
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Plex");
    private readonly ReaderWriterLockSlim _lock = new();
    private readonly ILogger<PlexServerManager> _logger = loggerFactory.CreateLogger<PlexServerManager>();
    private readonly Dictionary<string, (PlexServer, List<IPlexServer> Proxies)> _servers = [];

    public void Dispose()
    {
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
        this._httpClient.Dispose();
    }

    public IPlexServer? GetServer(string name)
    {
        if (!discovery.Servers.ContainsKey(name))
        {
            return null;
        }
        IPlexServer proxy;
        try
        {
            this._lock.EnterWriteLock();
            if (this._servers.GetValueOrDefault(name) is ({ } server, { } proxies))
            {
                proxies.Add(proxy = PlexServerProxy.Create(server));
            }
            else
            {
                this._servers[name] = (
                    server = new(name, discovery, tokenStore, this._httpClient, settingsManager, loggerFactory.CreateLogger<PlexServer>()),
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
                if (this._servers.GetValueOrDefault(name) is not ({ } server, { } proxies))
                {
                    return;
                }
                try
                {
                    this._lock.EnterWriteLock();
                    if (proxies.Remove(proxy) && proxies.Count == 0)
                    {
                        this._servers.Remove(name);
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
}
