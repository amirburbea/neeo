using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Exceptions;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Hisense;

public sealed class HisenseTV : IDisposable
{
    private readonly string _clientIdPrefix;
    private Connection? _connection;

    private bool _isDisposed;
    private Timer? _timer;

    private HisenseTV(IPAddress ipAddress, PhysicalAddress macAddress, string? clientIdPrefix)
    {
        this.IPAddress = ipAddress;
        this.MacAddress = macAddress;
        this._clientIdPrefix = clientIdPrefix ?? Dns.GetHostName();
    }

    public event EventHandler? Connected;

    public event EventHandler? Disconnected;

    public event EventHandler? Sleep;

    public event EventHandler<StateChangedEventArgs>? StateChanged;

    public event EventHandler<VolumeChangedEventArgs>? VolumeChanged;

    public IPAddress IPAddress { get; }

    public bool IsConnected => this._connection != null && this._connection.IsConnected;

    public PhysicalAddress MacAddress { get; }

    public static async Task<HisenseTV[]> DiscoverAsync(string? clientIdPrefix = default, CancellationToken cancellationToken = default)
    {
        ConcurrentBag<HisenseTV> bag = new();
        await Parallel.ForEachAsync(
            NetworkMethods.GetNetworkDevices(),
            cancellationToken,
            async (pair, cancellationToken) =>
            {
                try
                {
                    (IPAddress ipAddress, PhysicalAddress macAddress) = pair;
                    if (await HisenseTV.TryCreate(ipAddress, macAddress, true, clientIdPrefix, cancellationToken).ConfigureAwait(false) is { } tv)
                    {
                        bag.Add(tv);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ignore.
                }
            }
        ).ConfigureAwait(false);
        return bag.ToArray();
    }

    public static Task<HisenseTV?> DiscoverOneAsync(string? clientIdPrefix = default, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<HisenseTV?> taskCompletionSource = new();
        cancellationToken.Register(() => taskCompletionSource.TrySetCanceled(cancellationToken));
        ThreadPool.QueueUserWorkItem(_ => DiscoverOneAsync());
        return taskCompletionSource.Task;

        async void DiscoverOneAsync()
        {
            try
            {
                using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                await Parallel.ForEachAsync(
                    NetworkMethods.GetNetworkDevices(),
                    cts.Token,
                    async (pair, cancellationToken) =>
                    {
                        try
                        {
                            (IPAddress ipAddress, PhysicalAddress macAddress) = pair;
                            if (await HisenseTV.TryCreate(ipAddress, macAddress, true, clientIdPrefix, cancellationToken).ConfigureAwait(false) is { } tv)
                            {
                                taskCompletionSource.TrySetResult(tv);
                                cts.Cancel();
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Ignore.
                        }
                    }
                ).ConfigureAwait(false);
                taskCompletionSource.TrySetResult(default);
            }
            catch (OperationCanceledException)
            {
                // Ignore.
            }
        }
    }

    public static Task<HisenseTV?> TryCreate(PhysicalAddress macAddress, bool connectionRequired = false, string? clientIdPrefix = default, CancellationToken cancellationToken = default)
    {
        return NetworkMethods.GetNetworkDevices().Where(pair => pair.Value.Equals(macAddress)).Select(pair => pair.Key).FirstOrDefault() is { } ipAddress
            ? HisenseTV.TryCreate(ipAddress, macAddress, connectionRequired, clientIdPrefix, cancellationToken)
            : Task.FromResult(default(HisenseTV));
    }

    public Task<IState> AuthenticateAsync(string code)
    {
        return this._connection is { } connection
            ? connection.AuthenticateAsync(code)
            : throw new InvalidOperationException();
    }

    public async Task ChangeSourceAsync(string name, CancellationToken cancellationToken = default)
    {
        if (this._connection == null)
        {
            await this.TryConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        await (this._connection?.ChangeSourceAsync(name) ?? Task.CompletedTask).ConfigureAwait(false);
    }

    public async Task ChangeVolumeAsync(int value, CancellationToken cancellationToken = default)
    {
        if (this._connection == null)
        {
            await this.TryConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        await (this._connection?.ChangeVolumeAsync(value) ?? Task.CompletedTask).ConfigureAwait(false);
    }

    public void Dispose()
    {
        this._isDisposed = true;
        if (Interlocked.Exchange(ref this._timer, default) is { } timer)
        {
            timer.Dispose();
        }
        using Connection? connection = Interlocked.Exchange(ref this._connection, null);
        if (connection != null)
        {
            this.RemoveListeners(connection);
        }
    }

    public async Task<IState?> GetStateAsync(CancellationToken cancellationToken = default)
    {
        if (this._connection != null)
        {
            return await this._connection.GetStateAsync().ConfigureAwait(false);
        }
        if (await this.TryConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            return await this.GetStateAsync(cancellationToken);
        }
        return null;
    }

    public async Task<double> GetVolumeAsync(CancellationToken cancellationToken = default)
    {
        if (this._connection != null)
        {
            return await this._connection.GetVolumeAsync().ConfigureAwait(false);
        }
        if (await this.TryConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            return await this.GetVolumeAsync(cancellationToken).ConfigureAwait(false);
        }
        return 0d;
    }

    public async Task LaunchAppAsync(string name, CancellationToken cancellationToken = default)
    {
        if (this._connection != null)
        {
            await this._connection.LaunchAppAsync(name).ConfigureAwait(false);
        }
        else if (await this.TryConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            await this.LaunchAppAsync(name, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<bool> SendKeyAsync(RemoteKey key, CancellationToken cancellationToken = default)
    {
        if (this._connection == null)
        {
            return await this.TryConnectAsync(cancellationToken).ConfigureAwait(false) && await this.SendKeyAsync(key, cancellationToken).ConfigureAwait(false);
        }
        await this._connection.SendKeyAsync(key).ConfigureAwait(false);
        return true;
    }

    private static async Task<HisenseTV?> TryCreate(IPAddress ipAddress, PhysicalAddress macAddress, bool connectionRequired, string? clientIdPrefix, CancellationToken cancellationToken)
    {
        HisenseTV tv = new(ipAddress, macAddress, clientIdPrefix);
        if (await tv.TryConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            return tv;
        }
        if (!connectionRequired)
        {
            tv.StartReconnectTimer();
            return tv;
        }
        tv.Dispose();
        return default;
    }

    private void Connection_Disconnected(object? sender, EventArgs e)
    {
        this.RemoveListeners((Connection)sender!);
        this._connection = null;
        if (!this._isDisposed)
        {
            this.StartReconnectTimer();
        }
        this.Disconnected?.Invoke(this, e);
    }

    private void Connection_Sleep(object? sender, EventArgs e) => this.Sleep?.Invoke(this, e);

    private void Connection_StateChanged(object? sender, StateChangedEventArgs e) => this.StateChanged?.Invoke(this, e);

    private void Connection_VolumeChanged(object? sender, VolumeChangedEventArgs e) => this.VolumeChanged?.Invoke(this, e);

    private void RemoveListeners(Connection connection)
    {
        connection.Sleep -= this.Connection_Sleep;
        connection.StateChanged -= this.Connection_StateChanged;
        connection.VolumeChanged -= this.Connection_VolumeChanged;
        connection.Disconnected -= this.Connection_Disconnected;
    }

    private void StartReconnectTimer()
    {
        Timer timer = new(this.Timer_Tick);
        timer.Change(15000, 15000);
        this._timer = timer;
    }

    private void Timer_Tick(object? state)
    {
        if (!this._isDisposed)
        {
            this.TryConnectAsync().Wait();
        }
    }

    private async Task<bool> TryConnectAsync(CancellationToken cancellationToken = default)
    {
        if (this._isDisposed)
        {
            throw new ObjectDisposedException(this.GetType().FullName);
        }
        Connection connection = new(this.IPAddress, this.MacAddress, this._clientIdPrefix);
        if (!await connection.TryConnectAsync(cancellationToken).ConfigureAwait(false) || this._isDisposed)
        {
            connection.Dispose();
            return false;
        }
        this._connection = connection;
        connection.Sleep += this.Connection_Sleep;
        connection.Disconnected += this.Connection_Disconnected;
        connection.VolumeChanged += this.Connection_VolumeChanged;
        connection.StateChanged += this.Connection_StateChanged;
        if (Interlocked.Exchange(ref this._timer, default) is { } timer)
        {
            timer.Dispose();
        }
        this.Connected?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private sealed class Connection : IDisposable
    {
        private static readonly IMqttClientFactory _clientFactory = new MqttFactory();
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IMqttClient _client;

        private readonly IMqttClientOptions _options;

        private TaskCompletionSource<MqttApplicationMessage>? _taskCompletionSource;

        public Connection(IPAddress ipAddress, PhysicalAddress macAddress, string clientIdPrefix)
        {
            this._client = Connection._clientFactory.CreateMqttClient();
            this._options = new MqttClientOptionsBuilder()
                .WithClientId(Uri.EscapeDataString($"{clientIdPrefix}-{macAddress}"))
                .WithTcpServer(ipAddress.ToString(), 36669)
                .WithCredentials("hisenseservice", "multimqttservice")
                .WithCommunicationTimeout(TimeSpan.FromMilliseconds(500))
                .WithTls(parameters: new() { UseTls = true, AllowUntrustedCertificates = true, IgnoreCertificateChainErrors = true, IgnoreCertificateRevocationErrors = true })
                .Build();
        }

        public event EventHandler? Disconnected;

        public event EventHandler? Sleep;

        public event EventHandler<StateChangedEventArgs>? StateChanged;

        public event EventHandler<VolumeChangedEventArgs>? VolumeChanged;

        public bool IsConnected => this._client.IsConnected;

        public async Task<IState> AuthenticateAsync(string code)
        {
            MqttApplicationMessage message = await this.SendMessageAsync(this.GetPublishTopic("ui_service", "authenticationcode"), new { AuthNum = code }).ConfigureAwait(false);
            return JsonSerializer.Deserialize<SuccessPayload>(message.Payload, Connection._jsonOptions).Result != 1
                ? new State(StateType.AuthenticationRequired)
                : await this.GetStateAsync().ConfigureAwait(false);
        }

        public async Task ChangeSourceAsync(string name)
        {
            SourceInfo[] sources = await this.GetSourcesAsync().ConfigureAwait(false);
            if (Array.FindIndex(sources, source => source.Name == name) is int index and >= 0)
            {
                await this.SendMessageAsync(this.GetPublishTopic("ui_service", "changesource"), sources[index]).ConfigureAwait(false);
            }
        }

        public Task ChangeVolumeAsync(int volume)
        {
            if (volume is < 0 or > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0 and 100.");
            }
            return this.SendMessageAsync(this.GetPublishTopic("ui_service", "changevolume"), volume.ToString());
        }

        public void Dispose()
        {
            Debug.WriteLine($"Disposing {this._client.Options.ClientId}");
            this._client.Dispose();
        }

        public async Task<AppInfo[]> GetAppsAsync()
        {
            MqttApplicationMessage message = await this.SendMessageAsync(this.GetPublishTopic("ui_service", "applist")).ConfigureAwait(false);
            return JsonSerializer.Deserialize<AppInfo[]>(message.Payload, Connection._jsonOptions)!;
        }

        public async Task<SourceInfo[]> GetSourcesAsync()
        {
            MqttApplicationMessage message = await this.SendMessageAsync(this.GetPublishTopic("ui_service", "sourcelist")).ConfigureAwait(false);
            return JsonSerializer.Deserialize<SourceInfo[]>(message.Payload, Connection._jsonOptions)!;
        }

        public async Task<IState> GetStateAsync()
        {
            MqttApplicationMessage message = await this.SendMessageAsync(this.GetPublishTopic("ui_service", "gettvstate")).ConfigureAwait(false);
            return TranslateStateMessage(message);
        }

        public async Task<int> GetVolumeAsync()
        {
            MqttApplicationMessage message = await this.SendMessageAsync(this.GetPublishTopic("ui_service", "getvolume")).ConfigureAwait(false);
            return Connection.TranslateVolumeMessage(message);
        }

        public async Task LaunchAppAsync(string name)
        {
            AppInfo[] apps = await this.GetAppsAsync().ConfigureAwait(false);
            if (Array.FindIndex(apps, app => string.Equals(app.Name, name, StringComparison.OrdinalIgnoreCase)) is int index and not -1)
            {
                await this.SendMessageAsync(this.GetPublishTopic("ui_service", "launchapp"), apps[index]).ConfigureAwait(false);
            }
        }

        public Task SendKeyAsync(RemoteKey key) => this.SendMessageAsync(this.GetPublishTopic("remote_service", "sendkey"), TextAttribute.GetText(key), waitForNextMessage: false);

        public async Task<MqttApplicationMessage> SendMessageAsync(string topic, object? body = default, bool waitForNextMessage = true)
        {
            string payload = body switch
            {
                null => "{}",
                string text => text,
                _ => JsonSerializer.Serialize(body, Connection._jsonOptions)
            };
            Debug.WriteLine("sending message '{0}' to topic {1}.", payload, topic);
            MqttApplicationMessage message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build();
            await this._client.PublishAsync(message).ConfigureAwait(false);
            return waitForNextMessage ? await WaitForNextMessageAsync().ConfigureAwait(false) : message;

            async Task<MqttApplicationMessage> WaitForNextMessageAsync()
            {
                if (this._taskCompletionSource is { } source)
                {
                    return await source.Task.ConfigureAwait(false);
                }
                TaskCompletionSource<MqttApplicationMessage> taskCompletionSource = new();
                try
                {
                    return await (this._taskCompletionSource = taskCompletionSource).Task.ConfigureAwait(false);
                }
                finally
                {
                    this._taskCompletionSource = default;
                }
            }
        }

        public async Task<bool> TryConnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (await this._client.ConnectAsync(this._options, cancellationToken).ConfigureAwait(false) is { ResultCode: MqttClientConnectResultCode.Success })
                {
                    this._client.UseApplicationMessageReceivedHandler(this.OnMessageReceived);
                    this._client.UseDisconnectedHandler(this.OnDisconnected);
                    await this._client.SubscribeAsync("/remoteapp/mobile/#").ConfigureAwait(false);
                    return true;
                }
            }
            catch (MqttCommunicationException)
            {
                // Do nothing.
            }
            return false;
        }

        private static int TranslateVolumeMessage(MqttApplicationMessage message) => JsonSerializer.Deserialize<VolumeData>(message.Payload, Connection._jsonOptions).Value;

        private string GetDataTopic(string service, string action) => $"/remoteapp/mobile/{this._client.Options.ClientId}/{service}/data/{action}";

        private string GetPublishTopic(string service, string action) => $"/remoteapp/tv/{service}/{this._client.Options.ClientId}/actions/{action}";

        private Task OnDisconnected(MqttClientDisconnectedEventArgs e)
        {
            Debug.WriteLine("Disconnected: {0}", Enum.GetName(e.Reason));
            this.Disconnected?.Invoke(this, e);
            return Task.CompletedTask;
        }

        private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            string topic = e.ApplicationMessage.Topic;
            Debug.WriteLine("Received message on topic {0} ({1})", topic, e.ApplicationMessage.Payload is null ? string.Empty : Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
            if (this._taskCompletionSource is { } source)
            {
                source.TrySetResult(e.ApplicationMessage);
            }
            switch (topic)
            {
                case BroadcastTopics.Sleep:
                    this.Sleep?.Invoke(this, EventArgs.Empty);
                    break;
                case BroadcastTopics.VolumeChange:
                    this.VolumeChanged?.Invoke(this, new(Connection.TranslateVolumeMessage(e.ApplicationMessage)));
                    break;
                case BroadcastTopics.Launcher:
                case BroadcastTopics.Settings:
                case BroadcastTopics.State:
                    this.StateChanged?.Invoke(this, new(this.TranslateStateMessage(e.ApplicationMessage)));
                    break;
            }
            return e.AcknowledgeAsync(default);
        }

        private IState TranslateStateMessage(MqttApplicationMessage message)
        {
            switch (message.Topic)
            {
                case BroadcastTopics.Launcher:
                    return new State(StateType.Launcher);
                case BroadcastTopics.Settings:
                    return new State(StateType.Settings);
                case BroadcastTopics.State:
                    StateData data = JsonSerializer.Deserialize<StateData>(message.Payload, Connection._jsonOptions);
                    return data.Type switch
                    {
                        "livetv" => new State(StateType.LiveTV),
                        "app" => new AppState(new(data.Name!, data.Url!)),
                        _ => throw new(Encoding.UTF8.GetString(message.Payload)),
                    };
                case string topic when topic == this.GetDataTopic("ui_service", "authentication"):
                    return new State(StateType.AuthenticationRequired);
            }
            return new State(StateType.Unknown);
        }

        private static class BroadcastTopics
        {
            public const string Launcher = "/remoteapp/mobile/broadcast/ui_service/actions/remote_launcher";
            public const string Settings = "/remoteapp/mobile/broadcast/ui_service/actions/remote_setting";
            public const string Sleep = "/remoteapp/mobile/broadcast/platform_service/actions/tvsleep";
            public const string State = "/remoteapp/mobile/broadcast/ui_service/state";
            public const string VolumeChange = "/remoteapp/mobile/broadcast/platform_service/actions/volumechange";
        }
    }

    private record struct State(StateType Type) : IState;

    private record struct SuccessPayload(int Result);

    private record struct VolumeData(
        [property: JsonPropertyName("volume_type")] int Type,
        [property: JsonPropertyName("volume_value")] int Value
    );

    private record struct StateData(
        [property: JsonPropertyName("statetype")] string Type,
        string? Name = default,
        string? Url = default
    );
}