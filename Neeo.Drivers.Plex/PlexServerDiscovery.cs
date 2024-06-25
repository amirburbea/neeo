using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
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
            // Set when a server is found, or when we timeout after 1 second.
            using CancellationTokenSource searchCancellationTokenSource = new(TimeSpan.FromSeconds(1d));
            try
            {

                await Parallel.ForEachAsync(
                    NetworkMethods.GetNetworkDevices().Select(pair => pair.Key),
                    cancellationToken,
                    async (address, cancellationToken) =>
                    {
                        logger.LogInformation("Checking address {address}...", address);
                        using CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(
                            cancellationToken,
                            searchCancellationTokenSource.Token
                        );
                        IPlexServer plexServer = plexServerManager.GetServer(address);
                        try
                        {
                            await plexServer.InitializeAsync(source.Token).ConfigureAwait(false);
                            logger.LogInformation("Discovered server {id} at {address}", plexServer.DeviceId, plexServer.IPAddress);
                            tcs.TrySetResult(plexServer);
                            searchCancellationTokenSource.Cancel();
                            return;
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected when another server is found.
                        }
                        catch (HttpRequestException)
                        {
                            // Expected when server is not found.
                        }
                        catch (Exception e)
                        {
                            logger.LogError("Exception occurred: {error}", e);
                        }
                        plexServer.Dispose();
                    }
                ).ConfigureAwait(false);
                if (tcs.TrySetResult(null))
                {
                    logger.LogInformation("Did not find plex server.");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                tcs.TrySetCanceled(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Ignore.
                logger.LogInformation("Dafuq");
            }
            catch (Exception e)
            {
                logger.LogError("Error: {error}", e.Message);
                Debugger.Break();
            }
        }
    }

    public void Dispose() => this._httpClient.Dispose();
}
