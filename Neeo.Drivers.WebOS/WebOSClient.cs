using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Drivers.WebOS.Models;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.WebOS;

public sealed class WebOSClient(
    IPAddress ipAddress,
    PhysicalAddress macAddress,
    ILogger logger,
    string? clientKey = null,
    int port = 3000
) : IDisposable
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<WebOSResponse>> _pendingRequests = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, Action<JsonElement>> _subscriptions = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private CancellationTokenSource? _cancellationTokenSource;
    private WebOSPointerSocket? _pointerSocket;
    private ReconnectLoop? _reconnectLoop;
    private Uri? _workingEndpoint;
    private ClientWebSocket? _webSocket;
    private int _messageIdCounter;
    private bool _isRegistered;
    private bool _isDisposed;
    private string? _clientKey = clientKey;
    private TaskCompletionSource<string>? _registerTcs;

    public event EventHandler? Connected;

    public event EventHandler? Disconnected;

    public event EventHandler<DataEventArgs<int>>? VolumeChanged;

    public event EventHandler<DataEventArgs<string>>? ForegroundAppChanged;

    public event EventHandler<DataEventArgs<string>>? Registered;

    public string DeviceId => this.MacAddress.Equals(PhysicalAddress.None) ? this.IPAddress.ToString() : this.MacAddress.ToString();

    public IPAddress IPAddress => ipAddress;

    public PhysicalAddress MacAddress { get; } = macAddress;

    public int Port => this._workingEndpoint?.Port ?? port;

    public string? ClientKey => this._clientKey;

    public bool IsPaired => !string.IsNullOrEmpty(this._clientKey);

    public bool IsConnected => this._webSocket is { State: WebSocketState.Open } && this._isRegistered;

    public int Volume { get; private set; }

    public bool IsMuted { get; private set; }

    public string ForegroundApp { get; private set; } = string.Empty;

    /// <summary>
    /// Connects using the stored client key. Never prompts on the TV - an unpaired client fails (see <see cref="PairAsync"/>).
    /// </summary>
    public Task<bool> ConnectAsync(CancellationToken cancellationToken = default) => this.ConnectAsync(allowPairing: false, cancellationToken);

    /// <summary>
    /// Connects, showing the pairing prompt on the TV if no client key is stored yet (waiting up to 60 seconds for it to be accepted).
    /// </summary>
    public Task<bool> PairAsync(CancellationToken cancellationToken = default) => this.ConnectAsync(allowPairing: true, cancellationToken);

    /// <summary>
    /// Requests a (re)connection attempt as soon as possible, coalesced with any attempt in flight. A no-op once disposed or
    /// while unpaired.
    /// </summary>
    public void RequestReconnect()
    {
        if (!this._isDisposed && this.IsPaired && !this.IsConnected)
        {
            this.GetReconnectLoop().Start(immediate: true);
        }
    }

    private async Task<bool> ConnectAsync(bool allowPairing, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(this._isDisposed, this);
        if (this.IsConnected)
        {
            return true;
        }
        await this._connectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (this.IsConnected)
            {
                return true;
            }
            if (!allowPairing && !this.IsPaired)
            {
                logger.LogDebug("Not connecting to webOS TV at {ipAddress}: it has not been paired.", this.IPAddress);
                return false;
            }
            return await this.PerformConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            this._connectLock.Release();
        }
    }

    public async Task<int> SetVolumeAsync(int volume, CancellationToken cancellationToken = default)
    {
        volume = Math.Clamp(volume, 0, 100);
        await this.SendRequestAsync("ssap://audio/setVolume", new { volume }, cancellationToken).ConfigureAwait(false);
        this.Volume = volume;
        return volume;
    }

    public Task SetMuteAsync(bool mute, CancellationToken cancellationToken = default)
    {
        return this.SendRequestAsync("ssap://audio/setMute", new { mute }, cancellationToken);
    }

    public Task PowerOffAsync(CancellationToken cancellationToken = default)
    {
        return this.SendRequestAsync("ssap://system/turnOff", null, cancellationToken);
    }

    public async Task PowerOnAsync(CancellationToken cancellationToken = default)
    {
        if (this.IsConnected || this.MacAddress.Equals(PhysicalAddress.None))
        {
            return;
        }
        // Requires "Turn on via Wi-Fi/LAN" (or equivalent) on the TV.
        await this.MacAddress.WakeAsync(cancellationToken).ConfigureAwait(false);
        // Give the TV's network a moment to come up, then reconnect rather than waiting for the next backoff tick.
        _ = Task.Delay(TimeSpan.FromSeconds(3d), CancellationToken.None).ContinueWith(
            _ => this.RequestReconnect(),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default
        );
    }

    public async Task SendKeyAsync(Buttons button, CancellationToken cancellationToken = default)
    {
        if (WebOSRemoteKey.TryGetButtonName(button, out string buttonName))
        {
            if (await this.TrySendViaPointerSocketAsync(buttonName, cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }

        // Fallback for media transport / volume if pointer socket is unavailable
        Task fallback = button switch
        {
            Buttons.Play => this.SendRequestAsync("ssap://media.controls/play", null, cancellationToken),
            Buttons.Pause => this.SendRequestAsync("ssap://media.controls/pause", null, cancellationToken),
            Buttons.Stop => this.SendRequestAsync("ssap://media.controls/stop", null, cancellationToken),
            Buttons.Forward => this.SendRequestAsync("ssap://media.controls/fastForward", null, cancellationToken),
            Buttons.Reverse => this.SendRequestAsync("ssap://media.controls/rewind", null, cancellationToken),
            Buttons.VolumeUp => this.SendRequestAsync("ssap://audio/volumeUp", null, cancellationToken),
            Buttons.VolumeDown => this.SendRequestAsync("ssap://audio/volumeDown", null, cancellationToken),
            Buttons.MuteToggle => this.SendRequestAsync("ssap://audio/setMute", new { mute = !this.IsMuted }, cancellationToken),
            Buttons.ChannelUp => this.SendRequestAsync("ssap://tv/channelUp", null, cancellationToken),
            Buttons.ChannelDown => this.SendRequestAsync("ssap://tv/channelDown", null, cancellationToken),
            _ => Task.CompletedTask
        };
        await fallback.ConfigureAwait(false);
    }

    public async Task<WebOSAppInfo[]> GetAppsAsync(CancellationToken cancellationToken = default)
    {
        WebOSResponse response = await this.SendRequestAsync("ssap://com.webos.applicationLauncher/listApps", null, cancellationToken).ConfigureAwait(false);
        if (response.Payload.TryGetProperty("apps", out JsonElement appsElement) && appsElement.ValueKind == JsonValueKind.Array)
        {
            return appsElement.Deserialize<WebOSAppInfo[]>(WebOSJsonSerializerOptions.Default) ?? [];
        }
        return [];
    }

    public async Task<WebOSAppInfo[]> GetExternalInputsAsync(CancellationToken cancellationToken = default)
    {
        WebOSResponse response = await this.SendRequestAsync("ssap://tv/getExternalInputList", null, cancellationToken).ConfigureAwait(false);
        if (!response.Payload.TryGetProperty("devices", out JsonElement devicesElement) || devicesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        List<WebOSAppInfo> inputs = [];
        foreach (JsonElement dev in devicesElement.EnumerateArray())
        {
            string id = dev.TryGetProperty("id", out JsonElement idProp) ? idProp.GetString() ?? string.Empty : string.Empty;
            string label = dev.TryGetProperty("label", out JsonElement labelProp) ? labelProp.GetString() ?? id : id;
            string? icon = dev.TryGetProperty("icon", out JsonElement iconProp) ? iconProp.GetString() : null;
            if (!string.IsNullOrEmpty(id))
            {
                inputs.Add(new(id, label, icon));
            }
        }
        return [.. inputs];
    }

    public Task LaunchAppAsync(string appId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(appId);
        return this.SendRequestAsync("ssap://system.launcher/launch", new { id = appId }, cancellationToken);
    }

    public Task SwitchInputAsync(string inputId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(inputId);
        return this.SendRequestAsync("ssap://tv/switchInput", new { inputId }, cancellationToken);
    }

    public async Task<WebOSResponse> SendRequestAsync(string uri, object? payload = default, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(this._isDisposed, this);
        if (this._webSocket is not { State: WebSocketState.Open } socket)
        {
            throw new InvalidOperationException("Not connected to webOS TV.");
        }

        string id = $"req_{Interlocked.Increment(ref this._messageIdCounter)}";
        WebOSRequest request = new(id, "request", uri, payload);
        TaskCompletionSource<WebOSResponse> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        this._pendingRequests.TryAdd(id, tcs);

        try
        {
            await this.SendRawAsync(socket, request, cancellationToken).ConfigureAwait(false);
            return await tcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            this._pendingRequests.TryRemove(id, out _);
            throw;
        }
    }

    private async Task<bool> PerformConnectAsync(CancellationToken cancellationToken)
    {
        foreach (Uri endpoint in this.GetEndpoints())
        {
            // Only fall back to the other endpoint when this one couldn't be reached - a failure after the socket opened (e.g. a
            // pairing prompt that wasn't accepted) would just fail again there (and show a second prompt).
            if (await this.TryConnectToAsync(endpoint, cancellationToken).ConfigureAwait(false) is { } connected)
            {
                return connected;
            }
        }
        return false;
    }

    /// <summary>
    /// Newer webOS firmware only accepts the secure socket on 3001 (with a self-signed certificate), older firmware only the
    /// plain one on <see cref="Port"/> - try the one that last worked first.
    /// </summary>
    private IEnumerable<Uri> GetEndpoints()
    {
        Uri secure = new($"wss://{this.IPAddress}:3001");
        Uri plain = new($"ws://{this.IPAddress}:{port}");
        return this._workingEndpoint == plain ? [plain, secure] : [secure, plain];
    }

    /// <returns><see langword="true"/> if connected and registered, <see langword="false"/> if the socket opened but registration
    /// failed, or <see langword="null"/> if the socket could not be opened.</returns>
    private async Task<bool?> TryConnectToAsync(Uri endpoint, CancellationToken cancellationToken)
    {
        logger.LogInformation("Connecting to webOS TV at {endpoint}...", endpoint);
        ClientWebSocket socket = WebOSClient.CreateSocket(endpoint);
        CancellationTokenSource cts = new();
        bool opened = false;
        bool connected = false;
        try
        {
            await socket.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
            opened = true;
            this._webSocket = socket;
            this._cancellationTokenSource = cts;
            this._workingEndpoint = endpoint;

            _ = Task.Run(this.MessageLoopAsync, cts.Token);

            // Also ends with a timeout (an OperationCanceledException) if the pairing prompt isn't accepted.
            string key = await this.RegisterAsync(socket, cts.Token).ConfigureAwait(false);
            this._clientKey = key;
            this._isRegistered = true;

            await this.SubscribeToEventsAsync(cts.Token).ConfigureAwait(false);
            _ = this.EnsurePointerSocketAsync(cts.Token);
            connected = true;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Failed to connect/register with webOS TV at {endpoint}: {message}", endpoint, ex.Message);
        }
        finally
        {
            if (!connected)
            {
                if (ReferenceEquals(this._webSocket, socket))
                {
                    this.CleanUp();
                }
                else
                {
                    cts.Dispose();
                    socket.Dispose();
                }
            }
        }
        if (connected)
        {
            this.Connected?.Invoke(this, EventArgs.Empty);
            logger.LogInformation("Successfully connected and paired with webOS TV at {endpoint}.", endpoint);
            return true;
        }
        return opened ? false : null;
    }

    private static ClientWebSocket CreateSocket(Uri endpoint)
    {
        ClientWebSocket socket = new() { Options = { KeepAliveInterval = TimeSpan.FromSeconds(30d) } };
        if (endpoint.Scheme == Uri.UriSchemeWss)
        {
            // The TV presents a self-signed certificate; this only applies to this socket.
            socket.Options.RemoteCertificateValidationCallback = static (_, _, _, _) => true;
        }
        return socket;
    }

    private async Task<string> RegisterAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        TaskCompletionSource<string> registerTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        this._registerTcs = registerTcs;

        Dictionary<string, object?> payload = new()
        {
            ["forcePairing"] = false,
            ["pairingType"] = "PROMPT",
            ["manifest"] = new
            {
                manifestVersion = 1,
                appVersion = "1.0",
                signed = new
                {
                    created = "20140509",
                    appId = "com.neeo.driver.webos",
                    vendorId = "neeo",
                    localizedAppNames = new Dictionary<string, string> { { "", "NEEO Remote" } },
                    permissions = new[]
                    {
                        "LAUNCH", "LAUNCH_WEBAPP", "APP_TO_APP", "CLOSE", "TEST_OPEN", "TEST_PROTECTED",
                        "CONTROL_AUDIO", "CONTROL_DISPLAY", "CONTROL_INPUT_JOYSTICK", "CONTROL_INPUT_MEDIA_RECORDING",
                        "CONTROL_INPUT_MEDIA_PLAYBACK", "CONTROL_INPUT_TV", "READ_APP_STATUS", "READ_CURRENT_CHANNEL",
                        "READ_INPUT_DEVICE_LIST", "READ_NETWORK_STATUS", "READ_RUNNING_APPS", "READ_TV_CHANNEL_LIST",
                        "WRITE_NOTIFICATION_TOAST", "READ_POWER_STATE", "READ_COUNTRY_INFO"
                    },
                    serial = "2f930e2d3ecd0eb6e1234567"
                },
                permissions = new[]
                {
                    "LAUNCH", "LAUNCH_WEBAPP", "APP_TO_APP", "CLOSE", "TEST_OPEN", "TEST_PROTECTED",
                    "CONTROL_AUDIO", "CONTROL_DISPLAY", "CONTROL_INPUT_JOYSTICK", "CONTROL_INPUT_MEDIA_RECORDING",
                    "CONTROL_INPUT_MEDIA_PLAYBACK", "CONTROL_INPUT_TV", "READ_APP_STATUS", "READ_CURRENT_CHANNEL",
                    "READ_INPUT_DEVICE_LIST", "READ_NETWORK_STATUS", "READ_RUNNING_APPS", "READ_TV_CHANNEL_LIST",
                    "WRITE_NOTIFICATION_TOAST", "READ_POWER_STATE", "READ_COUNTRY_INFO"
                }
            }
        };

        if (!string.IsNullOrEmpty(this._clientKey))
        {
            payload["client-key"] = this._clientKey;
        }

        WebOSRequest registerRequest = new("register_0", "register", null, payload);
        await this.SendRawAsync(socket, registerRequest, cancellationToken).ConfigureAwait(false);

        // TV pairing prompt timeout
        using CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(60d));
        using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        return await registerTcs.Task.WaitAsync(linkedCts.Token).ConfigureAwait(false);
    }

    private async Task SubscribeToEventsAsync(CancellationToken cancellationToken)
    {
        await this.SubscribeAsync("sub_volume", "ssap://audio/getVolume", this.OnVolumePayload, cancellationToken).ConfigureAwait(false);
        await this.SubscribeAsync("sub_app", "ssap://com.webos.applicationLauncher/getForegroundAppInfo", this.OnAppPayload, cancellationToken).ConfigureAwait(false);
    }

    private async Task SubscribeAsync(string id, string uri, Action<JsonElement> handler, CancellationToken cancellationToken)
    {
        if (this._webSocket is not { State: WebSocketState.Open } socket)
        {
            return;
        }
        this._subscriptions[id] = handler;
        WebOSRequest request = new(id, "subscribe", uri, null);
        await this.SendRawAsync(socket, request, cancellationToken).ConfigureAwait(false);
    }

    private void OnVolumePayload(JsonElement payload)
    {
        if (payload.TryGetProperty("volume", out JsonElement volumeProp) && volumeProp.TryGetInt32(out int volume))
        {
            this.Volume = volume;
            this.VolumeChanged?.Invoke(this, volume);
        }
        if (payload.TryGetProperty("muted", out JsonElement mutedProp))
        {
            this.IsMuted = mutedProp.GetBoolean();
        }
    }

    private void OnAppPayload(JsonElement payload)
    {
        if (payload.TryGetProperty("appId", out JsonElement appProp) && appProp.GetString() is { } appId)
        {
            this.ForegroundApp = appId;
            this.ForegroundAppChanged?.Invoke(this, appId);
        }
    }

    private async Task<bool> TrySendViaPointerSocketAsync(string buttonName, CancellationToken cancellationToken)
    {
        try
        {
            if (this._pointerSocket is { IsConnected: true } socket)
            {
                await socket.SendButtonAsync(buttonName, cancellationToken).ConfigureAwait(false);
                return true;
            }

            if (await this.EnsurePointerSocketAsync(cancellationToken).ConfigureAwait(false) && this._pointerSocket is { IsConnected: true } newSocket)
            {
                await newSocket.SendButtonAsync(buttonName, cancellationToken).ConfigureAwait(false);
                return true;
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to send button {button} over pointer socket, will try fallback.", buttonName);
        }
        return false;
    }

    private async Task<bool> EnsurePointerSocketAsync(CancellationToken cancellationToken)
    {
        if (this._pointerSocket is { IsConnected: true })
        {
            return true;
        }
        try
        {
            WebOSResponse response = await this.SendRequestAsync("ssap://com.webos.service.networkinput/getPointerInputSocket", null, cancellationToken).ConfigureAwait(false);
            if (!response.Payload.TryGetProperty("socketPath", out JsonElement socketPathProp) || socketPathProp.GetString() is not { } socketPath)
            {
                return false;
            }

            Uri baseUri = this._workingEndpoint ?? new($"ws://{this.IPAddress}:{port}");
            Uri fullUri = Uri.IsWellFormedUriString(socketPath, UriKind.Absolute)
                ? new(socketPath)
                : new(baseUri, socketPath);

            WebOSPointerSocket pointer = new(fullUri, logger);
            await pointer.ConnectAsync(cancellationToken).ConfigureAwait(false);
            Interlocked.Exchange(ref this._pointerSocket, pointer)?.Dispose();
            return true;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to initialize pointer input socket.");
            return false;
        }
    }

    private async Task MessageLoopAsync()
    {
        try
        {
            if (this._cancellationTokenSource is { Token: { } token } && this._webSocket is { State: WebSocketState.Open } webSocket)
            {
                await JsonWebSocket.MessageLoop(
                    webSocket,
                    (response, _) =>
                    {
                        this.ProcessIncomingResponse(response);
                        return ValueTask.CompletedTask;
                    },
                    WebOSJsonSerializerContext.Default.WebOSResponse,
                    this.OnDisconnected,
                    cancellationToken: token
                ).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "webOS message loop terminated unexpectedly.");
        }
        finally
        {
            this.CancelOutstanding();
        }
    }

    private void ProcessIncomingResponse(WebOSResponse response)
    {
        if (response.Type == "registered")
        {
            string? key = response.Payload.TryGetProperty("client-key", out JsonElement keyProp) ? keyProp.GetString() : null;
            if (key != null)
            {
                this._clientKey = key;
                this.Registered?.Invoke(this, key);
            }
            this._registerTcs?.TrySetResult(key ?? string.Empty);
            return;
        }

        if (response.Id is { } id)
        {
            if (this._pendingRequests.TryRemove(id, out TaskCompletionSource<WebOSResponse>? taskSource))
            {
                if (response.Type == "error" || !string.IsNullOrEmpty(response.Error))
                {
                    taskSource.TrySetException(new ApplicationException($"webOS error: {response.Error}"));
                }
                else
                {
                    taskSource.TrySetResult(response);
                }
            }

            if (this._subscriptions.TryGetValue(id, out Action<JsonElement>? handler))
            {
                try
                {
                    handler(response.Payload);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error executing subscription handler for {id}.", id);
                }
            }
        }
    }

    private void OnDisconnected()
    {
        this.CleanUp();
        this.Disconnected?.Invoke(this, EventArgs.Empty);
        if (this._isDisposed)
        {
            return;
        }
        this.GetReconnectLoop().Start();
    }

    // One loop for the client's lifetime, created on first use (a field initializer can't capture this). A short initial delay
    // so a TV turned on with its own remote is picked up quickly; an unpaired client stops retrying (pairing connects it).
    private ReconnectLoop GetReconnectLoop()
    {
        if (Volatile.Read(ref this._reconnectLoop) is { } existing)
        {
            return existing;
        }
        ReconnectLoop loop = new(
            TimeSpan.FromSeconds(5d),
            TimeSpan.FromMinutes(2d),
            async cancellationToken => await this.ConnectAsync(cancellationToken).ConfigureAwait(false) || !this.IsPaired,
            logger
        );
        if (Interlocked.CompareExchange(ref this._reconnectLoop, loop, null) is { } winner)
        {
            loop.Dispose();
            return winner;
        }
        if (this._isDisposed)
        {
            loop.Dispose();
        }
        return loop;
    }

    private void CleanUp()
    {
        this._isRegistered = false;
        Interlocked.Exchange(ref this._pointerSocket, null)?.Dispose();
        this._cancellationTokenSource?.Cancel();
        this._cancellationTokenSource?.Dispose();
        this._cancellationTokenSource = null;
        this._webSocket?.Dispose();
        this._webSocket = null;
    }

    private void CancelOutstanding()
    {
        foreach ((string id, TaskCompletionSource<WebOSResponse> tcs) in this._pendingRequests)
        {
            tcs.TrySetCanceled();
        }
        this._pendingRequests.Clear();
        this._registerTcs?.TrySetCanceled();
    }

    private async Task SendRawAsync(WebSocket socket, WebOSRequest request, CancellationToken cancellationToken)
    {
        byte[] requestBytes = JsonSerializer.SerializeToUtf8Bytes(request, WebOSJsonSerializerOptions.Default);
        await this._sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await socket.SendAsync(requestBytes.AsMemory(), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            this._sendLock.Release();
        }
    }

    public void Dispose()
    {
        if (this._isDisposed)
        {
            return;
        }
        this._isDisposed = true;
        Interlocked.Exchange(ref this._reconnectLoop, null)?.Dispose();
        this.CleanUp();
        this.CancelOutstanding();
        this._connectLock.Dispose();
        this._sendLock.Dispose();
    }
}
