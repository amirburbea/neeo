using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

public interface IPlexServer : IDisposable
{
    string? AuthToken { get; set; }

    DiscoveredDevice DeviceDescriptor { get; }

    string DeviceId { get; }

    IPAddress IPAddress { get; }

    bool IsConnected { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);

    Task<HttpStatusCode?> GetStatusCodeAsync(CancellationToken cancellationToken = default);

    Task TryLoginAsync(string userName, string password, CancellationToken cancellationToken = default);
}

internal sealed partial class PlexServer : IPlexServer, IDisposable
{
    private static readonly Uri _signInUri = new("https://plex.tv/users/sign_in.json");

    private readonly string _clientIdentifier;
    private readonly string _fileName;
    private readonly IFileStore _fileStore;
    private readonly HttpClient _httpClient;
    private readonly ILogger<PlexServer> _logger;
    private readonly Uri _uri;
    private string? _authToken;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task<bool>? _connectTask;
    private ClientWebSocket? _serverWebSocket;

    public PlexServer(IPAddress ipAddress, HttpClient httpClient, string clientIdentifier, IFileStore fileStore, ILogger<PlexServer> logger)
    {
        this.IPAddress = ipAddress;
        this._uri = new($"http://{ipAddress}:32400");
        this._httpClient = httpClient;
        this._fileStore = fileStore;
        this._logger = logger;
        this._clientIdentifier = clientIdentifier;
        string hostName;
        string displayName;
        try
        {
            hostName = Dns.GetHostEntry(ipAddress).HostName;
            displayName = PlexServer.GetDnsSuffix() is { Length: not 0 } suffix && hostName.EndsWith($".{suffix}")
                ? hostName[..^(1 + suffix.Length)]
                : hostName;
        }
        catch (Exception)
        {
            displayName = hostName = ipAddress.ToString();
        }
        this._fileName = StringMethods.TitleCaseToSnakeCase(hostName.Replace('-', '_').Replace('.', '_'));
        try
        {
            if (fileStore.HasFile(this._fileName))
            {
                this._authToken = fileStore.ReadText(this._fileName);
            }
        }
        catch (Exception)
        {
            // Do nothing.
        }
        this.DeviceDescriptor = new(hostName, displayName, true);
    }

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
        Playqueues = 8,

        [Text("provider-playback")]
        ProviderPlayback = 16,
    }

    public string? AuthToken
    {
        get => this._authToken;
        set
        {
            if (value == this._authToken)
            {
                return;
            }
            this._authToken = value;
            if (value is null)
            {
                this._fileStore.DeleteFile(this._fileName);
            }
            else
            {
                this._fileStore.WriteText(this._fileName, value);
            }
        }
    }

    public DiscoveredDevice DeviceDescriptor { get; }

    public string DeviceId => this.DeviceDescriptor.Id;

    public IPAddress IPAddress { get; }

    public bool IsConnected => false;

    Task IPlexServer.ConnectAsync(CancellationToken cancellationToken) => this.ConnectAsync(cancellationToken);

    public void Dispose()
    {
        this.CleanUp();
        this.Destroyed?.Invoke(this, EventArgs.Empty);
    }

    public async Task<HttpStatusCode?> GetStatusCodeAsync(CancellationToken cancellationToken)
    {
        try
        {
            using CancellationTokenSource timeoutTokenSource = new(TimeSpan.FromSeconds(1d));
            using CancellationTokenSource junctionTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                timeoutTokenSource.Token
            );
            return await this._httpClient.HeadAsync(this._uri, this.ConfigureRequest, cancellationToken: junctionTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // We either previously found a server or had a connection timeout.
        }
        catch (HttpRequestException)
        {
            // Failed to connect to the server.
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Unexpected error while checking {address}", this.IPAddress);
        }
        return null;
    }

    public async Task TryLoginAsync(string userName, string password, CancellationToken cancellationToken)
    {
        JsonElement element = await this._httpClient.PostAsync<JsonElement>(
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
            request.Headers.Add("X-Plex-Client-Identifier", this._clientIdentifier);
        }
    }

    private static string GetDnsSuffix()
    {
        foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.OperationalStatus is OperationalStatus.Up && adapter.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211)
            {
                return adapter.GetIPProperties().DnsSuffix;
            }
        }
        return "";
    }

    private static async Task MessageLoop<TMessage>(
        WebSocket webSocket,
        Func<TMessage, CancellationToken, Task> processMessage,
        Action onDisconnected,
        CancellationToken cancellationToken = default
    ) where TMessage : notnull
    {
        byte[] previous = [];
        try
        {
            int previousLength = 0;
            using IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(32768);
            while (webSocket.State == WebSocketState.Open && await webSocket.ReceiveAsync(owner.Memory, cancellationToken).ConfigureAwait(false) is { MessageType: not WebSocketMessageType.Close } result)
            {
                // If a complete message was received in a single read, process it.
                if (result.EndOfMessage && previous is [])
                {
                    await ProcessAsync(owner.Memory.Span[..result.Count]).ConfigureAwait(false);
                    continue;
                }
                // Combine previous fragment with incoming one.
                int nextLength = previousLength + result.Count;
                byte[] next = ArrayPool<byte>.Shared.Rent(nextLength);
                if (previous is not [])
                {
                    previous.AsSpan(0, previousLength).CopyTo(next);
                    ArrayPool<byte>.Shared.Return(previous);
                }
                owner.Memory[..result.Count].CopyTo(next.AsMemory(previousLength));
                // If we still did not receive the complete
                if (!result.EndOfMessage)
                {
                    (previous, previousLength) = (next, nextLength);
                    continue;
                }
                (previous, previousLength) = ([], 0);
                try
                {
                    await ProcessAsync(next.AsSpan(0, nextLength)).ConfigureAwait(false);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(next);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // PlexServer was disposed.
            return;
        }
        catch (WebSocketException)
        {
            onDisconnected();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
        finally
        {
            if (previous is not [])
            {
                ArrayPool<byte>.Shared.Return(previous);
            }
        }

        Task ProcessAsync(ReadOnlySpan<byte> span) => processMessage(
            JsonSerializer.Deserialize<TMessage>(span, JsonSerialization.Options)!,
            cancellationToken
        );
    }

    private void CleanUp()
    {
        using WebSocket? webSocket = Interlocked.Exchange(ref this._serverWebSocket, default);
        using CancellationTokenSource? source = Interlocked.Exchange(ref this._cancellationTokenSource, default);
        source?.Cancel();
    }

    private void ConfigureRequest(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(this.AuthToken))
        {
            request.Headers.Add("X-Plex-Token", this.AuthToken);
        }
    }

    private async Task<bool> ConnectAsync(CancellationToken cancellationToken)
    {
        return !string.IsNullOrEmpty(this.AuthToken) && await (this._connectTask ??= ConnectAsync()).ConfigureAwait(false);

        async Task<bool> ConnectAsync()
        {
            // ClientWebSocket's keep-alive process is unsupported by the Plex Server, causing the server to close connections.
            // Set the keep-alive interval to infinite.
            string url = $"ws://{this.IPAddress}:32400/:/websockets/notifications";
            ClientWebSocket webSocket = new() { Options = { KeepAliveInterval = Timeout.InfiniteTimeSpan } };
            CancellationTokenSource cts = new();
            try
            {
                this._logger.LogInformation("Connecting to Plex server @ {url}...", url);
                await webSocket.ConnectAsync(new($"{url}?X-Plex-Token={this.AuthToken}"), cancellationToken).ConfigureAwait(false);
                this._serverWebSocket = webSocket;
                this._cancellationTokenSource = cts;
                _ = Task.Factory.StartNew(this.ServerMessageLoop, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                await this.OnConnectedAsync().ConfigureAwait(false);
                return true;
            }
            catch (WebSocketException)
            {
                this._logger.LogWarning("Failed to connect to Plex server {ipAddress}:32400!", this.IPAddress);
                cts.Dispose();
                webSocket.Dispose();
                return false;
            }
            finally
            {
                this._connectTask = null;
            }
        }
    }

    private async Task<ClientServer[]> GetClientsAsync(CancellationToken cancellationToken)
    {
        ClientsResponse response = await this._httpClient.GetAsync<ClientsResponse>(
            this._uri.Combine("clients"),
            this.ConfigureRequest,
            cancellationToken
        ).ConfigureAwait(false);
        return response.MediaContainer.Clients ?? [];
    }

    private async Task OnConnectedAsync()
    {
        if (this._serverWebSocket is not { } webSocket || this._cancellationTokenSource is not { Token: { } cancellationToken })
        {
            return;
        }
        this._logger.LogInformation("Connected to Plex server {ipAddress}:32400", this.IPAddress);
        var clients = await this.GetClientsAsync(cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"[{string.Join(',', clients.Where(client => (client.ProtocolCapabilities & ProtocolCapabilities.Playback) != 0))}]");
    }

    private void OnDisconnected()
    {
        this.CleanUp();
    }

    private async Task ServerMessageLoop()
    {
        if (this._cancellationTokenSource is { } cts && this._serverWebSocket is { State: WebSocketState.Open } webSocket)
        {
            await PlexServer.MessageLoop<ServerMessage>(
                webSocket,
                ProcessNotificatons,
                this.OnDisconnected,
                cts.Token
            ).ConfigureAwait(false);
        }

        static Task ProcessNotificatons(ServerMessage message, CancellationToken cancellationToken)
        {
            NotificationContainer container = message.Notifications;
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
