using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

/// <summary>
/// Utility for finding a Plex server on the local network.
/// </summary>
public interface IPlexServerDiscovery
{
    /// <summary>
    /// Attempts to find a Plex server on the local network.
    /// </summary>
    /// <returns>Task which resolves to an <see cref="IPlexServer"/> if found, otherwise <c>null</c>.</returns>
    Task<IPlexServer?> DiscoverAsync(CancellationToken cancellationToken = default);
}

internal sealed class PlexServerDiscovery(
    IHttpClientFactory httpClientFactory,
    IPlexServerManager plexServerManager,
    ILogger<PlexServerDiscovery> logger
) : IPlexServerDiscovery, IDisposable
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(nameof(Plex));

    public Task<IPlexServer?> DiscoverAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource<IPlexServer?> tcs = new();
        _ = DiscoverAsync();
        return tcs.Task;

        async Task DiscoverAsync()
        {
            logger.LogInformation("Discovering local plex server...");
            using CancellationTokenSource searchCancellationToken = new(); // Set when a server is found.
            await Parallel.ForEachAsync(
                NetworkMethods.GetNetworkDevices().Select(pair => pair.Key),
                cancellationToken,
                async (address, cancellationToken) =>
                {
                    IPlexServer plexServer = plexServerManager.GetServer(address);
                    if (await plexServer.GetStatusCodeAsync(cancellationToken).ConfigureAwait(false) is null)
                    {
                        plexServer.Dispose();
                        return;
                    }
                    logger.LogInformation("Discovered server {hostname} at {address}", plexServer.DeviceDescriptor.Id, plexServer.IPAddress);
                    tcs.TrySetResult(plexServer);
                    searchCancellationToken.Cancel();
                }
            ).ConfigureAwait(false);
            tcs.TrySetResult(null);
        }
    }

    public void Dispose() => this._httpClient.Dispose();
}
