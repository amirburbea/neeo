using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;

namespace Neeo.Drivers.Plex;

public interface IPlexServerManager
{
    IPlexServer? GetServer(string machineIdentifier);
}

internal sealed class PlexServerManager(
    IPlexServerDiscovery serverDiscovery,
    IPlexServerFactory serverFactory
) : IPlexServerManager, IDisposable
{
    private readonly Lock _lock = new();
    private readonly Dictionary<string, (IPlexServer Server, List<IPlexServer> Proxies)> _servers = [];

    public void Dispose()
    {
        List<IPlexServer> proxies;
        using (this._lock.EnterScope())
        {
            proxies = [.. this._servers.Values.SelectMany(tuple => tuple.Proxies)];
        }
        foreach (IPlexServer proxy in proxies)
        {
            proxy.Dispose();
        }
    }

    public IPlexServer? GetServer(string machineIdentifier)
    {
        if (!serverDiscovery.Data.ContainsKey(machineIdentifier))
        {
            return null;
        }
        IPlexServer proxy;
        using (this._lock.EnterScope())
        {
            if (this._servers.GetValueOrDefault(machineIdentifier) is ({ } server, { } proxies))
            {
                proxies.Add(proxy = PlexServerProxy.Create(server));
            }
            else
            {
                this._servers.Add(
                    machineIdentifier, 
                    (server = serverFactory.Create(machineIdentifier), [proxy = PlexServerProxy.Create(server)])
                );
            }
        }
        proxy.Disposed
            .Take(1)
            .Subscribe(_ => OnProxyDisposed());
        return proxy;

        void OnProxyDisposed()
        {
            using (this._lock.EnterScope())
            {
                if (this._servers.GetValueOrDefault(machineIdentifier) is not ({ } server, { } proxies))
                {
                    return;
                }
                if (proxies.Remove(proxy) && proxies.Count == 0)
                {
                    this._servers.Remove(machineIdentifier);
                    server.Dispose();
                }
            }
        }
    }
}
