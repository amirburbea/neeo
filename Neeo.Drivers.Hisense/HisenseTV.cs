using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Channels;
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
                (IPAddress ipAddress, PhysicalAddress macAddress) = pair;
                try
                {
                    if (await HisenseTV.TryCreate(ipAddress, macAddress, true, clientIdPrefix, cancellationToken).ConfigureAwait(false) is not { } tv)
                    {
                        return;
                    }
                    bag.Add(tv);
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
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                await Parallel.ForEachAsync(
                    NetworkMethods.GetNetworkDevices(),
                    cts.Token,
                    async (pair, cancellationToken) =>
                    {
                        (IPAddress ipAddress, PhysicalAddress macAddress) = pair;
                        try
                        {
                            if (await HisenseTV.TryCreate(ipAddress, macAddress, true, clientIdPrefix, cancellationToken).ConfigureAwait(false) is not { } tv)
                            {
                                return;
                            }
                            taskCompletionSource.TrySetResult(tv);
                            cts.Cancel();
                        }
                        catch (OperationCanceledException)
                        {
                            // Ignore.
                        }
                    }
                ).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            taskCompletionSource.TrySetResult(default);
        }
    }

    public static async Task<HisenseTV?> TryCreate(PhysicalAddress macAddress, bool connectionRequired = false, string? clientIdPrefix = default, CancellationToken cancellationToken = default)
    {
        foreach ((IPAddress ipAddress, PhysicalAddress physicalAddress) in NetworkMethods.GetNetworkDevices())
        {
            if (macAddress.Equals(physicalAddress))
            {
                return await HisenseTV.TryCreate(ipAddress, macAddress, connectionRequired, clientIdPrefix, cancellationToken).ConfigureAwait(false);
            }
        }
        return default;
    }

    public Task<IState> AuthenticateAsync(string code, CancellationToken cancellationToken = default)
    {
        return this._connection == null
            ? throw new InvalidOperationException()
            : this._connection.AuthenticateAsync(code, cancellationToken);
    }

    public async Task LaunchAppAsync(string name, CancellationToken cancellationToken = default)
    {
        if (this._connection != null)
        {
            await this._connection.LaunchAppAsync(name, cancellationToken).ConfigureAwait(false);
        }
        else if (await this.TryConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            await this.LaunchAppAsync(name, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task ChangeSourceAsync(string name, CancellationToken cancellationToken = default)
    {
        if (this._connection != null)
        {
            await this._connection.ChangeSourceAsync(name, cancellationToken).ConfigureAwait(false);
        }
        else if (await this.TryConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            await this.ChangeSourceAsync(name, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task ChangeVolumeAsync(int value, CancellationToken cancellationToken = default)
    {
        if (this._connection != null)
        {
            await this._connection.ChangeVolumeAsync(value).ConfigureAwait(false);
        }
        else if (await this.TryConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            await this.ChangeVolumeAsync(value, cancellationToken).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        this._isDisposed = true;
        if (Interlocked.Exchange(ref this._timer, default) is { } timer)
        {
            timer.Dispose();
        }
        using Connection? connection = Interlocked.Exchange(ref this._connection, null);
        this.RemoveListeners(connection);
    }

    [Obsolete("DELETE AND MAKE CLASS PRIVATE")]
    public Connection? GetConnection() => this._connection;

    public async Task<IState?> GetStateAsync(CancellationToken cancellationToken = default)
    {
        if (this._connection != null)
        {
            return await this._connection.GetStateAsync(cancellationToken).ConfigureAwait(false);
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
            return await this._connection.GetVolumeAsync(cancellationToken).ConfigureAwait(false);
        }
        if (await this.TryConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            return await this.GetVolumeAsync(cancellationToken).ConfigureAwait(false);
        }
        return 0d;
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
        this.RemoveListeners(sender as Connection);
        this._connection = null;
        if (!this._isDisposed)
        {
            this.StartReconnectTimer();
        }
    }

    private void Connection_Sleep(object? sender, EventArgs e) => this.Sleep?.Invoke(this, e);

    private void Connection_StateChanged(object? sender, StateChangedEventArgs e) => this.StateChanged?.Invoke(this, e);

    private void Connection_VolumeChanged(object? sender, VolumeChangedEventArgs e) => this.VolumeChanged?.Invoke(this, e);

    private void RemoveListeners(Connection? connection)
    {
        if (connection == null)
        {
            return;
        }
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

    private async void Timer_Tick(object? state)
    {
        if (!this._isDisposed)
        {
            await this.TryConnectAsync();
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
        return true;
    }

    public sealed class Connection : IDisposable
    {
        private static readonly IMqttClientFactory _clientFactory = new MqttFactory();
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        private static readonly string _launcherTopic = Connection.GetBroadcastTopic("ui_service", "actions/remote_launcher");
        private static readonly string _settingsTopic = GetBroadcastTopic("ui_service", "actions/remote_setting");
        private static readonly string _sleepTopic = GetBroadcastTopic("platform_service", "actions/tvsleep");
        private static readonly string _stateTopic = GetBroadcastTopic("ui_service", suffix: "state");
        private static readonly string[] _stateTopics = new[] { Connection._stateTopic, Connection._launcherTopic, Connection._settingsTopic };
        private static readonly string _volumeChangeTopic = GetBroadcastTopic("platform_service", suffix: "actions/volumechange");

        private readonly ConcurrentDictionary<string, Channel<MqttApplicationMessage>> _channels = new();
        private readonly IMqttClient _client;
        private readonly IMqttClientOptions _options;

        public Connection(IPAddress ipAddress, PhysicalAddress macAddress, string clientIdPrefix)
        {
            this._client = Connection._clientFactory.CreateMqttClient();
            this._options = new MqttClientOptionsBuilder()
                .WithClientId(Uri.EscapeDataString($"{clientIdPrefix}-{macAddress}"))
                .WithTcpServer(ipAddress.ToString(), 36669)
                .WithCredentials("hisenseservice", "multimqttservice")
                .WithTls(parameters: new() { UseTls = true, AllowUntrustedCertificates = true, IgnoreCertificateChainErrors = true, IgnoreCertificateRevocationErrors = true })
                .Build();
        }

        public event EventHandler? Disconnected;

        public event EventHandler? Sleep;

        public event EventHandler<StateChangedEventArgs>? StateChanged;

        public event EventHandler<VolumeChangedEventArgs>? VolumeChanged;

        public bool IsConnected => this._client.IsConnected;

        public async Task<IState> AuthenticateAsync(string code, CancellationToken cancellationToken = default)
        {
            const string action = "authenticationcode";
            const string service = "ui_service";
            await this.SendMessageAsync(this.GetPublishTopic(service, action), new { AuthNum = code }).ConfigureAwait(false);
            MqttApplicationMessage message = await this.WaitForMessageAsync(this.GetDataTopic(service, action), cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<SuccessPayload>(message.Payload, Connection._jsonOptions).Result != 1
                ? new State(StateType.AuthenticationRequired)
                : await this.GetStateAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task ChangeSourceAsync(string name, CancellationToken cancellationToken)
        {
            const string service = "ui_service";
            const string action = "changesource";
            SourceInfo[] sources = await this.GetSourcesAsync(cancellationToken);
            if (Array.FindIndex(sources, source => source.Name == name) is int index and >= 0)
            {
                await this.SendMessageAsync(this.GetPublishTopic(service, action), sources[index]).ConfigureAwait(false);
            }
        }

        public Task ChangeVolumeAsync(int volume)
        {
            const string service = "ui_service";
            const string action = "changevolume";
            if (volume is < 0 or > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0 and 100.");
            }
            return this.SendMessageAsync(this.GetPublishTopic(service, action), volume.ToString());
        }

        public void Dispose()
        {
            Debug.WriteLine($"Disposing {this._client.Options.ClientId}");
            this._client.Dispose();
        }

        public async Task<AppInfo[]> GetAppsAsync(CancellationToken cancellationToken = default)
        {
            const string service = "ui_service";
            const string action = "applist";
            await this.SendMessageAsync(this.GetPublishTopic(service, action)).ConfigureAwait(false);
            MqttApplicationMessage message = await this.WaitForMessageAsync(this.GetDataTopic(service, action), cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<AppInfo[]>(message.Payload, Connection._jsonOptions)!;
        }

        public async Task<SourceInfo[]> GetSourcesAsync(CancellationToken cancellationToken = default)
        {
            const string service = "ui_service";
            const string action = "sourcelist";
            await this.SendMessageAsync(this.GetPublishTopic(service, action)).ConfigureAwait(false);
            MqttApplicationMessage message = await this.WaitForMessageAsync(this.GetDataTopic(service, action), cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<SourceInfo[]>(message.Payload, Connection._jsonOptions)!;
        }

        public async Task<IState> GetStateAsync(CancellationToken cancellationToken = default)
        {
            const string service = "ui_service";
            await this.SendMessageAsync(this.GetPublishTopic(service, "gettvstate")).ConfigureAwait(false);
            string authenticationRequiredTopic = this.GetDataTopic(service, "authentication");
            string[] topics = new string[Connection._stateTopics.Length + 1];
            topics[0] = authenticationRequiredTopic;
            Connection._stateTopics.AsSpan().CopyTo(topics.AsSpan(1));
            MqttApplicationMessage message = await this.WaitForFirstMessageAsync(topics, cancellationToken).ConfigureAwait(false);
            return message.Topic == authenticationRequiredTopic
                ? new State(StateType.AuthenticationRequired)
                : TranslateStateMessage(message);
        }

        public async Task<int> GetVolumeAsync(CancellationToken cancellationToken = default)
        {
            const string action = "getvolume";
            await this.SendMessageAsync(this.GetPublishTopic("ui_service", action)).ConfigureAwait(false);
            MqttApplicationMessage message = await this.WaitForMessageAsync(this.GetDataTopic("platform_service", action), cancellationToken).ConfigureAwait(false);
            return Connection.TranslateVolumeMessage(message);
        }

        public async Task LaunchAppAsync(string name, CancellationToken cancellationToken = default)
        {
            AppInfo[] apps = await this.GetAppsAsync(cancellationToken).ConfigureAwait(false);
            if (Array.FindIndex(apps, app => string.Equals(app.Name, name, StringComparison.OrdinalIgnoreCase)) is int index and not -1)
            {
                await this.SendMessageAsync(this.GetPublishTopic("ui_service", "launchapp"), apps[index]).ConfigureAwait(false);
            }
        }

        public Task SendKeyAsync(RemoteKey key) => this.SendMessageAsync(this.GetPublishTopic("remote_service", "sendkey"), TextAttribute.GetText(key));

        public async Task SendMessageAsync(string topic, object? body = default)
        {
            string payload = body switch
            {
                null => "{}",
                string text => text,
                _ => JsonSerializer.Serialize(body, Connection._jsonOptions)
            };
            Debug.WriteLine("sending message '{0}' to topic {1}.", payload, topic);
            await this._client.PublishAsync(topic, payload).ConfigureAwait(false);
        }

        public async Task<bool> TryConnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (await this._client.ConnectAsync(this._options, cancellationToken).ConfigureAwait(false) is { ResultCode: MqttClientConnectResultCode.Success })
                {
                    this._client.UseApplicationMessageReceivedHandler(this.OnMessageReceived);
                    this._client.UseDisconnectedHandler(this.OnDisconnected);
                    await this._client.SubscribeAsync("/remoteapp/mobile/#");
                    return true;
                }
            }
            catch (MqttCommunicationException)
            {
                // Do nothing.
            }
            return false;
        }

        private static string GetBroadcastTopic(string service, string suffix) => $"/remoteapp/mobile/broadcast/{service}/{suffix}";

        private static IState TranslateStateMessage(MqttApplicationMessage message)
        {
            if (message.Topic == Connection._launcherTopic)
            {
                return new State(StateType.Launcher);
            }
            if (message.Topic == Connection._settingsTopic)
            {
                return new State(StateType.Settings);
            }
            StateData data = JsonSerializer.Deserialize<StateData>(message.Payload, Connection._jsonOptions);
            return data.Type switch
            {
                "livetv" => new State(StateType.LiveTV),
                "app" => new AppState(new(data.Name!, data.Url!)),
                _ => throw new(Encoding.UTF8.GetString(message.Payload)),
            };
        }

        private static int TranslateVolumeMessage(MqttApplicationMessage message) => JsonSerializer.Deserialize<VolumeData>(message.Payload, Connection._jsonOptions).Value;

        private Channel<MqttApplicationMessage> GetChannel(string topic) => this._channels.GetOrAdd(topic, _ => Channel.CreateUnbounded<MqttApplicationMessage>(new() { SingleWriter = true }));

        private string GetDataTopic(string service, string action) => $"/remoteapp/mobile/{this._client.Options.ClientId}/{service}/data/{action}";

        private string GetPublishTopic(string service, string action) => $"/remoteapp/tv/{service}/{this._client.Options.ClientId}/actions/{action}";

        private Task OnDisconnected(MqttClientDisconnectedEventArgs e)
        {
            Debug.WriteLine("Disconnected: {0}", Enum.GetName(e.Reason));
            this.Disconnected?.Invoke(this, e);
            return Task.CompletedTask;
        }

        private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            string topic = e.ApplicationMessage.Topic;
            Debug.WriteLine("Received message on topic {0} ({1})", (object)topic, e.ApplicationMessage.Payload is null ? string.Empty : Encoding.UTF8.GetString(e.ApplicationMessage.Payload));
            await this.GetChannel(topic).Writer.WriteAsync(e.ApplicationMessage).ConfigureAwait(false);
            if (topic == Connection._volumeChangeTopic)
            {
                this.VolumeChanged?.Invoke(this, new(Connection.TranslateVolumeMessage(e.ApplicationMessage)));
            }
            else if (topic == Connection._sleepTopic)
            {
                this.Sleep?.Invoke(this, EventArgs.Empty);
            }
            else if (Array.IndexOf(_stateTopics, topic) != -1)
            {
                this.StateChanged?.Invoke(this, new(TranslateStateMessage(e.ApplicationMessage)));
            }
            await e.AcknowledgeAsync(default).ConfigureAwait(false);
        }

        private async Task<MqttApplicationMessage> WaitForFirstMessageAsync(string[] topics, CancellationToken cancellationToken = default)
        {
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task<MqttApplicationMessage>? task = default;
            try
            {
                task = await Task.WhenAny(Array.ConvertAll(topics, topic => this.WaitForMessageAsync(topic, cts.Token))).ConfigureAwait(false);
                cts.Cancel();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // do nothing.
            }
            return task!.Result;
        }

        private async Task<MqttApplicationMessage> WaitForMessageAsync(string topic, CancellationToken cancellationToken = default) => await this.GetChannel(topic).Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
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

public record struct SourceInfo([property: JsonPropertyName("sourcename")] string Name, [property: JsonPropertyName("sourceid")] int SourceId, [property: JsonPropertyName("displayname")] string DisplayName);