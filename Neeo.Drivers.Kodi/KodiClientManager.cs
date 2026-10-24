using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BitFaster.Caching.Lru;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities;
using Zeroconf;

namespace Neeo.Drivers.Kodi;

[SettingsStore("kodicredentials.json.bin")]
public sealed partial class KodiClientManager(
    ILogger<KodiClient> logger,
    IConfiguration configuration,
    ISettingsStore<KodiClientManager> settings,
    IHttpClientFactory httpClientFactory,
    IKodiHostLocator hostLocator
) : IDisposable
{
    private readonly ConcurrentLru<string, (byte[] Bytes, string ContentType)> _imageCache = new(
        configuration.GetValue<int>("Kodi:ImageCacheCapacity") is > 0 and { } configured ? configured : 64
    );
    private readonly UrlTokenStore _imageUrlTokens = new();
    private readonly ConcurrentDictionary<string, KodiClient> _clients = [];
    private readonly ConcurrentDictionary<IPAddress, Task<KodiClient?>> _connectingClients = new();
    private readonly Lock _initLock = new();
    private AuthenticationHeaderValue? _authorizationHeader = KodiClientManager.CreateAuthorizationHeader(settings);
    private Task? _initializationTask;

    public event EventHandler<DataEventArgs<KodiClient>>? ClientAdded;

    public IReadOnlyCollection<KodiClient> Clients => [.. this._clients.Values];

    public bool HasCredentials => settings.ContainsKey("username") && settings.ContainsKey("password");

    public void SetCredentials(string userName, string password)
    {
        this._authorizationHeader = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}"))
        );
        settings.Update(dict =>
        {
            dict["username"] = userName;
            dict["password"] = password;
        });
    }

    public Task DiscoverAsync(int scanTime = 5000, CancellationToken cancellationToken = default) => this.DiscoverAsync(scanTime, default, cancellationToken);

    public async Task DiscoverAsync(int scanTime, Func<KodiClient, bool>? considerDiscoveryComplete, CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        ConcurrentBag<Task> hostTasks = [];
        Task zeroconfTask = hostLocator.ScanAsync(
            TimeSpan.FromMilliseconds(scanTime),
            host => hostTasks.Add(OnHostDiscoveredAsync(host)),
            cts.Token
        );
        List<string> candidateHosts = [];
        if (configuration.GetSection("Kodi:Hosts").Get<string[]>() is { Length: > 0 } configHosts)
        {
            candidateHosts.AddRange(configHosts);
        }
        else if (configuration.GetValue<string>("Kodi:Host") is { Length: > 0 } singleHost)
        {
            candidateHosts.Add(singleHost);
        }
        candidateHosts.Add("kodi.local");
        Task probeTask = Task.Run(async () =>
        {
            foreach (string host in candidateHosts.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (cts.IsCancellationRequested)
                {
                    break;
                }
                try
                {
                    if (await this.TryAddHostAsync(host, cancellationToken: cts.Token).ConfigureAwait(false)
                        && considerDiscoveryComplete != null
                        && this.Clients.FirstOrDefault(c => c.DisplayName.Contains(host, StringComparison.OrdinalIgnoreCase)) is { } c
                        && considerDiscoveryComplete(c))
                    {
                        await cts.CancelAsync().ConfigureAwait(false);
                        break;
                    }
                }
                catch
                {
                    // Ignore probe errors
                }
            }
        }, CancellationToken.None);
        try
        {
            await Task.WhenAll(zeroconfTask, probeTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Discovery was cut short because considerDiscoveryComplete was satisfied.
        }
        await Task.WhenAll(hostTasks).ConfigureAwait(false);

        async Task OnHostDiscoveredAsync(IZeroconfHost host)
        {
            try
            {
                if (KodiClientManager.TryGetIPAddress(host) is not { } ipAddress)
                {
                    logger.LogWarning("Discovered Zeroconf host '{name}' without a valid IP address, skipping.", host.DisplayName);
                    return;
                }
                int? port = host.Services.TryGetValue(ZeroconfKodiHostLocator.HttpServiceName, out IService? service)
                    ? service.Port
                    : host.Services.Values.FirstOrDefault()?.Port;
                if (port is null or 0)
                {
                    logger.LogWarning("Discovered Zeroconf host '{name}' without a valid service port, skipping.", host.DisplayName);
                    return;
                }
                if (await this.GetOrConnectClientAsync(host.DisplayName, ipAddress, port.Value, cts.Token).ConfigureAwait(false) is { } client
                    && considerDiscoveryComplete != null
                    && considerDiscoveryComplete(client))
                {
                    await cts.CancelAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                // Ignore cancellation.
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error processing discovered Kodi host '{name}'.", host.DisplayName);
            }
        }
    }

    public async Task<bool> TryAddHostAsync(string hostNameOrAddress, int port = 8080, CancellationToken cancellationToken = default)
    {
        if (!IPAddress.TryParse(hostNameOrAddress, out IPAddress? ip))
        {
            ip = await hostLocator.ResolveAsync(hostNameOrAddress, cancellationToken).ConfigureAwait(false);
        }
        if (ip is null)
        {
            return false;
        }

        string displayName = hostNameOrAddress.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            ? $"Kodi ({hostNameOrAddress[..^6]})"
            : $"Kodi ({ip})";

        KodiClient? client = await this.GetOrConnectClientAsync(displayName, ip, port, cancellationToken).ConfigureAwait(false);
        return client is not null;
    }

    private Task<KodiClient?> GetOrConnectClientAsync(string displayName, IPAddress ipAddress, int port, CancellationToken cancellationToken)
    {
        if (this._clients.Values.FirstOrDefault(c => c.IPAddress.Equals(ipAddress)) is { } existing)
        {
            return Task.FromResult<KodiClient?>(existing);
        }

        return this._connectingClients.GetOrAdd(ipAddress, ConnectInternalAsync);

        async Task<KodiClient?> ConnectInternalAsync(IPAddress targetIp)
        {
            try
            {
                if (this._clients.Values.FirstOrDefault(c => c.IPAddress.Equals(targetIp)) is { } client)
                {
                    return client;
                }

                logger.LogInformation("Connecting to Kodi at {displayName} ({targetIp}:{port})...", displayName, targetIp, port);
                KodiClient newClient = new(displayName, targetIp, port, logger, configuration.GetValue("Kodi:WebSocketPort", KodiClient.DefaultWebSocketPort));
                if (!await newClient.ConnectAsync(cancellationToken).ConfigureAwait(false) || newClient.MacAddress.Equals(PhysicalAddress.None))
                {
                    logger.LogWarning("Failed to connect or retrieve MAC address from Kodi at {targetIp}.", targetIp);
                    newClient.Dispose();
                    return null;
                }
                if (this._clients.TryRemove(newClient.DeviceId, out KodiClient? oldClient))
                {
                    oldClient.Dispose();
                }
                if (!this._clients.TryAdd(newClient.DeviceId, newClient))
                {
                    // Another connection registered the same device in the meantime - keep that one.
                    newClient.Dispose();
                    return this._clients.GetValueOrDefault(newClient.DeviceId);
                }
                this.ClientAdded?.Invoke(this, newClient);
                logger.LogInformation("Successfully connected and registered Kodi '{displayName}' ({id}) at {targetIp}.", displayName, newClient.DeviceId, targetIp);
                return newClient;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to connect to Kodi at {targetIp}.", targetIp);
                return null;
            }
            finally
            {
                this._connectingClients.TryRemove(targetIp, out _);
            }
        }
    }

    public async Task<bool> ValidateCredentialsAsync(string userName, string password, CancellationToken cancellationToken = default)
    {
        IPAddress? targetIp = this._clients.Values.FirstOrDefault()?.IPAddress;
        int port = this._clients.Values.FirstOrDefault()?.HttpPort ?? 8080;
        if (targetIp is null && configuration.GetValue<string>("Kodi:Host") is { Length: > 0 } singleHost && IPAddress.TryParse(singleHost, out IPAddress? parsedIp))
        {
            targetIp = parsedIp;
        }
        if (targetIp is null)
        {
            await this.DiscoverAsync(2000, cancellationToken: cancellationToken).ConfigureAwait(false);
            targetIp = this._clients.Values.FirstOrDefault()?.IPAddress;
            port = this._clients.Values.FirstOrDefault()?.HttpPort ?? 8080;
        }
        if (targetIp is null)
        {
            return false;
        }
        try
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            using HttpRequestMessage request = new(HttpMethod.Post, $"http://{targetIp}:{port}/jsonrpc")
            {
                Content = new StringContent("{\"jsonrpc\":\"2.0\",\"method\":\"JSONRPC.Ping\",\"id\":1}", Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new(
                "Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}"))
            );
            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to validate Kodi credentials against {targetIp}:{port}.", targetIp, port);
            return false;
        }
    }

    private static AuthenticationHeaderValue? CreateAuthorizationHeader(ISettingsStore<KodiClientManager> settings) =>
        settings.TryGetValue("username", out string? user) && settings.TryGetValue("password", out string? pass) && user.Length > 0 && pass.Length > 0
            ? new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{user}:{pass}")))
            : null;

    public async Task<(byte[] Bytes, string ContentType)?> FetchImageAsync(string imageUrl, int maxDimension = 480, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxDimension, 0);
        string cacheKey = $"{maxDimension}:{imageUrl}";
        if (this._imageCache.TryGet(cacheKey, out (byte[] Bytes, string ContentType) cached))
        {
            return cached;
        }
        try
        {
            using HttpClient httpClient = httpClientFactory.CreateClient();
            using HttpRequestMessage request = new(HttpMethod.Get, imageUrl) { Headers = { Authorization = this._authorizationHeader } };
            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to fetch Kodi image from '{url}' (status: {statusCode}).", imageUrl, response.StatusCode);
                return null;
            }
            using Stream responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            byte[] resizedBytes = KodiImageResizer.Resize(responseStream, maxDimension);
            (byte[] Bytes, string ContentType) result = (resizedBytes, "image/jpeg");
            this._imageCache.AddOrUpdate(cacheKey, result);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to proxy Kodi image from '{url}'.", imageUrl);
            return null;
        }
    }

    public string RegisterImageUrl(string imageUrl) => this._imageUrlTokens.Register(imageUrl);

    public string? ResolveImageUrl(string token) => this._imageUrlTokens.Resolve(token);

    [GeneratedRegex(@"^(?<ip>(\d+[.]){3}\d+)(:|$)", RegexOptions.ExplicitCapture)]
    private static partial Regex IPAddressRegex();

    private static IPAddress? TryGetIPAddress(IZeroconfHost host)
    {
        if (!string.IsNullOrEmpty(host.IPAddress) && IPAddress.TryParse(host.IPAddress, out IPAddress? ip))
        {
            return ip;
        }
        if (host.IPAddresses is { Count: > 0 } addresses)
        {
            foreach (string addrStr in addresses)
            {
                if (IPAddress.TryParse(addrStr, out ip))
                {
                    return ip;
                }
            }
        }
        if (!string.IsNullOrEmpty(host.Id) && KodiClientManager.IPAddressRegex().Match(host.Id) is { Success: true, Groups: { } groups } && IPAddress.TryParse(groups["ip"].Value, out ip))
        {
            return ip;
        }
        return null;
    }

    public void Dispose()
    {
        foreach (KodiClient client in this._clients.Values)
        {
            client.Dispose();
        }
    }

    public KodiClient? GetClientOrDefault(string id) => this._clients.GetValueOrDefault(id);

    public Task InitializeAsync(string? deviceId = default, CancellationToken cancellationToken = default)
    {
        lock (this._initLock)
        {
            return this._initializationTask ??= InitializeDiscoveryAsync();
        }

        async Task InitializeDiscoveryAsync()
        {
            try
            {
                // Use a short initial window, consider initialization complete if a device is discovered.
                await this.DiscoverAsync(2000, client => deviceId is null || deviceId == client.DeviceId, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Ignore.
            }
        }
    }
}
