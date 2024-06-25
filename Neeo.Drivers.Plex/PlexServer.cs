using System;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Drivers.Plex.ServerNotifications;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

public interface IPlexServer : IDisposable
{
    string? AuthToken { get; set; }

    DiscoveredDevice DeviceDescriptor { get; }

    string DeviceId { get; }

    IPAddress IPAddress { get; }

    Task<HttpStatusCode> GetStatusCodeAsync(CancellationToken cancellationToken = default);

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task SubscribeAsync(CancellationToken cancellationToken = default);

    Task TryLoginAsync(string userName, string password, CancellationToken cancellationToken = default);
}

internal sealed partial class PlexServer(
    IPAddress ipAddress,
    string? dnsSuffix,
    HttpClient httpClient,
    IPlexDriverSettings driverSettings,
    ILogger<PlexServer> logger
) : IPlexServer, IDisposable
{
    private static readonly Uri _signInUri = new("https://plex.tv/users/sign_in.json");

    private readonly Uri _uri = new($"http://{ipAddress}:32400");
    private CancellationTokenSource? _cancellationTokenSource;
    private Task<bool>? _connectTask;
    private string? _hostName;
    private ClientWebSocket? _webSocket;

    internal event EventHandler? Destroyed;

    [Flags, JsonConverter(typeof(TextJsonConverter<ProtocolCapabilities>))]
    private enum ProtocolCapabilities
    {
        None = 0,

        [Text("playback")]
        Playback = 1,

        [Text("navigation")]
        Navigation = 2,

        [Text("timeline")]
        Timeline = 4,

        [Text("playqueues")]
        PlayQueues = 8,

        [Text("provider-playback")]
        ProviderPlayback = 16,
    }

    public string? AuthToken
    {
        get => this.ServerSettings.AuthToken;
        set
        {
            if (value != this.AuthToken)
            {
                this.ServerSettings = this.ServerSettings with { AuthToken = value };
            }
        }
    }

    public DiscoveredDevice DeviceDescriptor => new(this.DeviceId, this.DisplayName);

    public string DeviceId => this._hostName ?? this.IPAddress.ToString();

    public string DisplayName => dnsSuffix is { Length: > 0 } suffix && this._hostName is { } name && name.EndsWith($".{suffix}")
        ? name[..^(1 + suffix.Length)]
        : this.DeviceId;

    public IPAddress IPAddress => ipAddress;

    public PlexServerSettings ServerSettings
    {
        get => driverSettings.Servers.GetOrAdd(this.DeviceId);
        private set => driverSettings.Servers[this.DeviceId] = value;
    }

    public void Dispose()
    {
        this.CleanUp();
        this.Destroyed?.Invoke(this, EventArgs.Empty);
    }

    Task<HttpStatusCode> IPlexServer.GetStatusCodeAsync(CancellationToken cancellationToken) => this.GetStatusCodeAsync(true, cancellationToken: cancellationToken);

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await this.GetStatusCodeAsync(false, cancellationToken).ConfigureAwait(false);
        if (this._hostName is null)
        {
            await this.ResolveHostNameAsync(ipAddress, logger, cancellationToken).ConfigureAwait(false);
        }
    }

    Task IPlexServer.SubscribeAsync(CancellationToken cancellationToken) => this.ConnectAsync(cancellationToken);

    public async Task TryLoginAsync(string userName, string password, CancellationToken cancellationToken)
    {
        JsonElement element = await httpClient.PostAsync<JsonElement>(
            PlexServer._signInUri,
            configureRequest: ConfigureRequest,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);
        this.AuthToken = element.GetProperty("user").GetProperty("authToken").GetString()!;

        void ConfigureRequest(HttpRequestMessage request)
        {
            request.Headers.Authorization = new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}")));
            request.Headers.Add("X-Plex-Version", "1");
            request.Headers.Add("X-Plex-Product", Assembly.GetExecutingAssembly().GetName().Name);
            request.Headers.Add("X-Plex-Client-Identifier", driverSettings.ClientIdentifier);
        }
    }

    private void CleanUp()
    {
        using WebSocket? webSocket = Interlocked.Exchange(ref this._webSocket, default);
        using CancellationTokenSource? source = Interlocked.Exchange(ref this._cancellationTokenSource, default);
        source?.Cancel();
    }

    private void ConfigureRequest(HttpRequestMessage request)
    {
        if (this.AuthToken is { Length: not 0 } token)
        {
            request.Headers.Add("X-Plex-Token", token);
        }
    }

    private async Task<bool> ConnectAsync(CancellationToken cancellationToken)
    {
        return !string.IsNullOrEmpty(this.AuthToken) && await (this._connectTask ??= ConnectAsync()).ConfigureAwait(false);

        async Task<bool> ConnectAsync()
        {
            // ClientWebSocket's keep-alive process is unsupported by the Plex Server, causing the server to close connections.
            // Set the keep-alive interval to infinite.
            string url = $"ws://{ipAddress}:32400/:/websockets/notifications";
            ClientWebSocket webSocket = new() { Options = { KeepAliveInterval = Timeout.InfiniteTimeSpan } };
            CancellationTokenSource cts = new();
            try
            {
                logger.LogInformation("Connecting to Plex server @ {url}...", url);
                await webSocket.ConnectAsync(new($"{url}?X-Plex-Token={this.AuthToken}"), cancellationToken).ConfigureAwait(false);
                this._webSocket = webSocket;
                this._cancellationTokenSource = cts;
                _ = Task.Factory.StartNew(ServerMessageLoop, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                await this.OnConnectedAsync().ConfigureAwait(false);
                return true;
            }
            catch (WebSocketException)
            {
                logger.LogWarning("Failed to connect to Plex server {ipAddress}:32400!", ipAddress);
                cts.Dispose();
                webSocket.Dispose();
                return false;
            }
            finally
            {
                this._connectTask = null;
            }
        }

        async Task ServerMessageLoop()
        {
            if (this._cancellationTokenSource is not { Token: { } cancellationToken } || this._webSocket is not { State: WebSocketState.Open } webSocket)
            {
                return;
            }
            byte[] previous = [];
            try
            {
                int previousLength = 0;
                using IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(32 * 1024); // 32K
                do
                {
                    if (await webSocket.ReceiveAsync(owner.Memory, cancellationToken).ConfigureAwait(false) is not { MessageType: not WebSocketMessageType.Close } result)
                    {
                        break;
                    }
                    // If a complete message was received in a single read, process it.
                    if (result.EndOfMessage && previous.Length is 0)
                    {
                        await this.ProcessMessageAsync(owner.Memory.Span[..result.Count], cancellationToken).ConfigureAwait(false);
                        continue;
                    }
                    // Combine previous fragment with incoming one.
                    int nextLength = previousLength + result.Count;
                    byte[] next = ArrayPool<byte>.Shared.Rent(nextLength);
                    if (previous.Length is not 0)
                    {
                        previous.AsSpan(0, previousLength).CopyTo(next);
                        ArrayPool<byte>.Shared.Return(previous);
                    }
                    owner.Memory[..result.Count].CopyTo(next.AsMemory(previousLength));
                    // If we still did not receive the complete message.
                    if (!result.EndOfMessage)
                    {
                        (previous, previousLength) = (next, nextLength);
                        continue;
                    }
                    (previous, previousLength) = ([], 0);
                    try
                    {
                        await this.ProcessMessageAsync(next.AsSpan(0, nextLength), cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(next);
                    }
                } while (webSocket.State == WebSocketState.Open);
            }
            catch (OperationCanceledException)
            {
                // PlexServer was disposed.
                return;
            }
            catch (WebSocketException)
            {
                this.OnDisconnected();
            }
            finally
            {
                if (previous.Length is not 0)
                {
                    ArrayPool<byte>.Shared.Return(previous);
                }
            }
        }
    }

    private async Task<ClientServer[]> GetClientsAsync(CancellationToken cancellationToken)
    {
        ClientsResponse response = await httpClient.GetAsync<ClientsResponse>(
            this._uri.Combine("clients"),
            this.ConfigureRequest,
            cancellationToken
        ).ConfigureAwait(false);
        return response.MediaContainer.Clients ?? [];
    }

    private Task<HttpStatusCode> GetStatusCodeAsync(bool configureRequest, CancellationToken cancellationToken = default)
    {
        return httpClient.HeadAsync(this._uri, configureRequest ? this.ConfigureRequest : null, cancellationToken);
    }

    private async Task OnConnectedAsync()
    {
        if (this._webSocket is not { } webSocket || this._cancellationTokenSource is not { Token: { } cancellationToken })
        {
            return;
        }
        logger.LogInformation("Connected to Plex server {ipAddress}:32400", ipAddress);
        ClientServer[] clients = await this.GetClientsAsync(cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"[{string.Join(',', clients.Where(client => (client.ProtocolCapabilities & ProtocolCapabilities.Playback) != 0))}]");
    }

    private void OnDisconnected()
    {
        this.CleanUp();
    }

    private Task ProcessMessageAsync(ReadOnlySpan<byte> bytes, CancellationToken cancellationToken)
    {
        NotificationContainer container = JsonSerializer.Deserialize<ServerMessage>(bytes, JsonSerialization.WebOptions).Notifications;
        switch (container.Type)
        {
            case ServerNotificationType.Activity:
                Console.WriteLine(">>>ACTIVITY");
                Console.WriteLine(string.Join(',', container.ActivityNotifications!));
                break;
            case ServerNotificationType.Playing:
                Console.WriteLine(">>>PLAYING");
                Console.WriteLine(string.Join(',', container.PlaySessionStateNotifications!));
                break;
            case ServerNotificationType.StateChange:
                Console.WriteLine(">>>STATECHANGE");
                Console.WriteLine(string.Join(',', container.StatusNotifications!));
                break;

            default:
                Console.WriteLine("???" + container.Type + ": " + string.Join(",", container.AdditionalData.Select(pair => $"{pair.Key}={pair.Value}")));
                break;
        }
        return Task.CompletedTask;
    }

    private async Task ResolveHostNameAsync(IPAddress ipAddress, ILogger<PlexServer> logger, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Resolving host entry for {address}...", ipAddress);
            // Resolving host name is usually quick enough to be used synchronously but occasionally hangs. Use a 1 second timeout.
            using CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            source.CancelAfter(TimeSpan.FromSeconds(1d));
            this._hostName = (await Dns.GetHostEntryAsync(ipAddress.ToString(), source.Token).ConfigureAwait(false)).HostName;
        }
        catch (Exception)
        {
            this._hostName = ipAddress.ToString();
        }
    }

    private record struct ClientsResponse([property: JsonPropertyName(nameof(MediaContainer))] MediaContainer MediaContainer);

    private record struct MediaContainer([property: JsonPropertyName("Server")] ClientServer[]? Clients);

    private record struct ClientServer(
        string Name,
        string Host,
        string Address,
        int Port,
        string MachineIdentifier,
        string Version,
        string Protocol,
        string Product,
        string DeviceClass,
        double ProtocolVersion,
        ProtocolCapabilities ProtocolCapabilities
    );
}
