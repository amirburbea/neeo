using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Exceptions;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Hisense;

public sealed class HisenseTV : IDisposable
{
    private readonly string _clientIdPrefix;
    private readonly ILogger _logger;
    private readonly bool _useCertificates;

    private Connection? _connection;
    private bool _isDisposed;
    private PeriodicTimer? _reconnectTimer;

    private HisenseTV(IPAddress ipAddress, PhysicalAddress macAddress, ILogger logger, bool useCertificates, string? clientIdPrefix)
    {
        this.IPAddress = ipAddress;
        this.MacAddress = macAddress;
        this._logger = logger;
        this._useCertificates = useCertificates;
        this._clientIdPrefix = clientIdPrefix ?? Dns.GetHostName();
    }

    public event EventHandler? Connected;

    public event EventHandler? Disconnected;

    public event EventHandler? Sleep;

    public event EventHandler<DataEventArgs<IState>>? StateChanged;

    public event EventHandler<DataEventArgs<int>>? VolumeChanged;

    public string DeviceId => this.MacAddress.ToString();

    public IPAddress IPAddress { get; }

    public bool IsConnected => this._connection != null && this._connection.IsConnected;

    public PhysicalAddress MacAddress { get; }

    public static async Task<HisenseTV[]> DiscoverAsync(ILogger logger, bool useCertificates, string? clientIdPrefix = default, CancellationToken cancellationToken = default)
    {
        ConcurrentBag<HisenseTV> bag = [];
        await Task.WhenAll(
            NetworkMethods.GetNetworkDevices().Select(async pair =>
            {
                try
                {
                    (IPAddress ipAddress, PhysicalAddress macAddress) = pair;
                    if (await HisenseTV.TryCreate(ipAddress, macAddress, logger, true, useCertificates, clientIdPrefix, cancellationToken).ConfigureAwait(false) is { } tv)
                    {
                        bag.Add(tv);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Ignore.
                }
            })
        ).ConfigureAwait(false);
        return [.. bag];
    }

    public static Task<HisenseTV?> DiscoverOneAsync(ILogger logger, bool useCertificates, string? clientIdPrefix = default, CancellationToken cancellationToken = default)
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
                await Task.WhenAll(
                    NetworkMethods.GetNetworkDevices().Select(async pair =>
                    {
                        try
                        {
                            (IPAddress ipAddress, PhysicalAddress macAddress) = pair;
                            if (await HisenseTV.TryCreate(ipAddress, macAddress, logger, true, useCertificates, clientIdPrefix, cts.Token).ConfigureAwait(false) is { } tv)
                            {
                                taskCompletionSource.TrySetResult(tv);
                                cts.Cancel();
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Ignore.
                        }
                    })
                ).ConfigureAwait(false);
                taskCompletionSource.TrySetResult(default);
            }
            catch (OperationCanceledException)
            {
                // Ignore.
            }
        }
    }

    public static Task<HisenseTV?> TryCreateAsync(PhysicalAddress macAddress, ILogger logger, bool connectionRequired = false, bool useCertificates = false, string? clientIdPrefix = default, CancellationToken cancellationToken = default)
    {
        return NetworkMethods.GetNetworkDevices().Where(pair => pair.Value.Equals(macAddress)).Select(static pair => pair.Key).FirstOrDefault() is { } ipAddress
            ? HisenseTV.TryCreate(ipAddress, macAddress, logger, connectionRequired, useCertificates, clientIdPrefix, cancellationToken)
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
        if (!this.IsConnected)
        {
            await this.TryConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        await (this._connection?.ChangeSourceAsync(name) ?? Task.CompletedTask).ConfigureAwait(false);
    }

    public async Task ChangeVolumeAsync(int value, CancellationToken cancellationToken = default)
    {
        if (!this.IsConnected)
        {
            await this.TryConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        await (this._connection?.ChangeVolumeAsync(value) ?? Task.CompletedTask).ConfigureAwait(false);
    }

    public void Dispose()
    {
        this._isDisposed = true;
        if (Interlocked.Exchange(ref this._reconnectTimer, default) is { } timer)
        {
            timer.Dispose();
        }
        using Connection? connection = Interlocked.Exchange(ref this._connection, null);
        if (connection != null)
        {
            this.RemoveListeners(connection);
        }
    }

    public async Task<AppInfo[]> GetAppsAsync(CancellationToken cancellationToken = default)
    {
        if (!this.IsConnected)
        {
            await this.TryConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        return await (this._connection?.GetAppsAsync() ?? Task.FromResult(Array.Empty<AppInfo>())).ConfigureAwait(false);
    }

    public async Task<SourceInfo[]> GetSourcesAsync(CancellationToken cancellationToken = default)
    {
        if (!this.IsConnected)
        {
            await this.TryConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        return await (this._connection?.GetSourcesAsync() ?? Task.FromResult(Array.Empty<SourceInfo>())).ConfigureAwait(false);
    }

    public async Task<IState?> GetStateAsync(CancellationToken cancellationToken = default)
    {
        if (!this.IsConnected)
        {
            await this.TryConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        return this._connection == null ? null : await this._connection.GetStateAsync().ConfigureAwait(false);
    }

    public async Task<int> GetVolumeAsync(CancellationToken cancellationToken = default)
    {
        if (!this.IsConnected)
        {
            await this.TryConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        return await (this._connection?.GetVolumeAsync() ?? Task.FromResult(0)).ConfigureAwait(false);
    }

    public async Task LaunchAppAsync(string name, CancellationToken cancellationToken = default)
    {
        if (!this.IsConnected)
        {
            await this.TryConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        await (this._connection?.LaunchAppAsync(name) ?? Task.CompletedTask).ConfigureAwait(false);
    }

    public Task SendKeyAsync(RemoteKey key, CancellationToken cancellationToken = default) => this.SendKeyAsync(TextAttribute.GetText(key), cancellationToken);

    internal async Task SendKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!this.IsConnected)
        {
            await this.TryConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        await (this._connection?.SendKeyAsync(key) ?? Task.CompletedTask).ConfigureAwait(false);
    }

    private static async Task<HisenseTV?> TryCreate(IPAddress ipAddress, PhysicalAddress macAddress, ILogger logger, bool connectionRequired, bool useCertificates, string? clientIdPrefix, CancellationToken cancellationToken)
    {
        HisenseTV tv = new(ipAddress, macAddress, logger, useCertificates, clientIdPrefix);
        if (await tv.TryConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            return tv;
        }
        if (connectionRequired)
        {
            tv.Dispose();
            return null;
        }
        tv.StartReconnectTimer();
        return tv;
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

    private void Connection_StateChanged(object? sender, DataEventArgs<IState> e) => this.StateChanged?.Invoke(this, e);

    private void Connection_VolumeChanged(object? sender, DataEventArgs<int> e) => this.VolumeChanged?.Invoke(this, e);

    private void RemoveListeners(Connection connection)
    {
        connection.Sleep -= this.Connection_Sleep;
        connection.StateChanged -= this.Connection_StateChanged;
        connection.VolumeChanged -= this.Connection_VolumeChanged;
        connection.Disconnected -= this.Connection_Disconnected;
    }

    private void StartReconnectTimer()
    {
        this._reconnectTimer = new(TimeSpan.FromSeconds(14d));
        _ = Task.Factory.StartNew(
            async () =>
            {
                while (await this._reconnectTimer.WaitForNextTickAsync().ConfigureAwait(false))
                {
                    if (await this.TryConnectAsync().ConfigureAwait(false))
                    {
                        break;
                    }
                }
            },
            TaskCreationOptions.LongRunning
        ).ContinueWith(_ => Interlocked.Exchange(ref this._reconnectTimer, default)?.Dispose(), TaskContinuationOptions.ExecuteSynchronously);
    }

    private async Task<bool> TryConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(this._isDisposed, this);
        Connection connection = new(this.IPAddress, this.MacAddress, this._logger, this._useCertificates, this._clientIdPrefix);
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
        if (Interlocked.Exchange(ref this._reconnectTimer, default) is { } timer)
        {
            timer.Dispose();
        }
        this.Connected?.Invoke(this, EventArgs.Empty);
        return true;
    }

    private sealed class Connection(IPAddress ipAddress, PhysicalAddress macAddress, ILogger logger, bool useCertificates, string clientIdPrefix) : IDisposable
    {
        private static readonly MqttFactory _clientFactory = new();
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IMqttClient _client = Connection._clientFactory.CreateMqttClient();

        private readonly MqttClientOptions _options = new MqttClientOptionsBuilder()
                .WithClientId(Uri.EscapeDataString($"{clientIdPrefix}-{macAddress}"))
                .WithTcpServer(ipAddress.ToString(), 36669)
                .WithCredentials("hisenseservice", "multimqttservice")
                .WithTimeout(TimeSpan.FromMilliseconds(750))
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(30))
                .WithTls(parameters =>
                {
                    parameters.UseTls = true;
                    parameters.CertificateValidationHandler = _ => true;
                    if (useCertificates)
                    {
                        parameters.Certificates = [Connection.LoadCertificate()];
                    }
                })
                .Build();

        public event EventHandler? Disconnected;

        public event EventHandler? Sleep;

        public event EventHandler<DataEventArgs<IState>>? StateChanged;

        public event EventHandler<DataEventArgs<int>>? VolumeChanged;

        private event EventHandler<DataEventArgs<(string, string)>>? MessageReceived;

        public bool IsConnected => this._client.IsConnected;

        public async Task<IState> AuthenticateAsync(string code)
        {
            (_, string payload) = await this.SendMessageAsync(this.GetPublishTopic("ui_service", "authenticationcode"), new { AuthNum = code }).ConfigureAwait(false);
            return payload.Length == 0 || JsonSerializer.Deserialize<SuccessPayload>(payload, Connection._jsonOptions).Result != 1
                ? new State(StateType.AuthenticationRequired)
                : await this.GetStateAsync().ConfigureAwait(false);
        }

        public async Task ChangeSourceAsync(string name)
        {
            if (Array.Find(await this.GetSourcesAsync().ConfigureAwait(false), source => source.Name == name) is { } source)
            {
                await this.SendMessageAsync(this.GetPublishTopic("ui_service", "changesource"), source, waitForNextMessage: false).ConfigureAwait(false);
            }
        }

        public Task ChangeVolumeAsync(int volume)
        {
            return volume is < 0 or > 100
                ? throw new ArgumentOutOfRangeException(nameof(volume), "Volume must be between 0 and 100.")
                : this.SendMessageAsync(this.GetPublishTopic("ui_service", "changevolume"), volume.ToString());
        }

        public void Dispose()
        {
            logger.LogInformation("Disposing {clientId}.", this._options.ClientId);
            this._client.Dispose();
        }

        public async Task<AppInfo[]> GetAppsAsync()
        {
            (_, string payload) = await this.SendMessageAsync(this.GetPublishTopic("ui_service", "applist")).ConfigureAwait(false);
            return JsonSerializer.Deserialize<AppInfo[]>(payload, Connection._jsonOptions)!;
        }

        public async Task<SourceInfo[]> GetSourcesAsync()
        {
            (_, string payload) = await this.SendMessageAsync(this.GetPublishTopic("ui_service", "sourcelist")).ConfigureAwait(false);
            return JsonSerializer.Deserialize<SourceInfo[]>(payload, Connection._jsonOptions)!;
        }

        public async Task<IState> GetStateAsync()
        {
            (string topic, string payload) = await this.SendMessageAsync(this.GetPublishTopic("ui_service", "gettvstate")).ConfigureAwait(false);
            return this.TranslateStateMessage(topic, payload);
        }

        public async Task<int> GetVolumeAsync()
        {
            (_, string payload) = await this.SendMessageAsync(this.GetPublishTopic("ui_service", "getvolume")).ConfigureAwait(false);
            return Connection.TranslateVolumePayload(payload);
        }

        public async Task LaunchAppAsync(string name)
        {
            if (Array.Find(await this.GetAppsAsync().ConfigureAwait(false), app => string.Equals(app.Name, name, StringComparison.OrdinalIgnoreCase)) is { } app)
            {
                await this.SendMessageAsync(this.GetPublishTopic("ui_service", "launchapp"), app).ConfigureAwait(false);
            }
        }

        public Task SendKeyAsync(string key) => this.SendMessageAsync(this.GetPublishTopic("remote_service", "sendkey"), key, waitForNextMessage: false);

        public Task<(string, string)> SendMessageAsync(string topic, bool waitForNextMessage = true) => this.SendMessageAsync(topic, default(object), waitForNextMessage);

        public async Task<(string, string)> SendMessageAsync<TBody>(string topic, TBody? body, bool waitForNextMessage = true)
        {
            string payload = body switch
            {
                null => string.Empty,
                string text => text,
                _ => JsonSerializer.Serialize(body, Connection._jsonOptions)
            };
            logger.LogInformation("Sending message '{payload}' to topic '{topic}'.", payload, topic);
            if (!waitForNextMessage)
            {
                await PublishAsync().ConfigureAwait(false);
                return (string.Empty, string.Empty);
            }
            TaskCompletionSource<(string, string)> taskCompletionSource = new();
            try
            {
                this.MessageReceived += OnMessageReceived;
                await PublishAsync().ConfigureAwait(false);
                return await taskCompletionSource.Task.ConfigureAwait(false);
            }
            finally
            {
                this.MessageReceived -= OnMessageReceived;
            }

            void OnMessageReceived(object? sender, DataEventArgs<(string, string)> e) => taskCompletionSource.TrySetResult(e.Data);

            Task PublishAsync() => this._client.PublishAsync(new() { Topic = topic, Payload = Encoding.UTF8.GetBytes(payload) });
        }

        public async Task<bool> TryConnectAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (await this._client.ConnectAsync(this._options, cancellationToken).ConfigureAwait(false) is { ResultCode: MqttClientConnectResultCode.Success })
                {
                    this._client.ApplicationMessageReceivedAsync += this.OnMessageReceived;
                    this._client.DisconnectedAsync += this.OnDisconnected;
                    await this._client.SubscribeAsync("/remoteapp/mobile/#", cancellationToken: cancellationToken).ConfigureAwait(false);
                    return true;
                }
            }
            catch (MqttCommunicationException)
            {
                // Do nothing.
            }
            return false;
        }

        private static X509Certificate2 LoadCertificate()
        {
            using Stream certificateStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(HisenseTV).Namespace}.Certificates.rcm_certchain_pem.cer")!;
            using StreamReader certificateReader = new(certificateStream);
            using Stream privateKeyStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"{typeof(HisenseTV).Namespace}.Certificates.rcm_pem_privkey.pkcs8")!;
            using StreamReader privateKeyReader = new(privateKeyStream);
            return X509Certificate2.CreateFromPem(certPem: certificateReader.ReadToEnd(), keyPem: privateKeyReader.ReadToEnd());
        }

        private static int TranslateVolumePayload(string payload) => JsonSerializer.Deserialize<VolumeData>(payload, Connection._jsonOptions).Value;

        private string GetDataTopic(string service, string action) => $"/remoteapp/mobile/{this._options.ClientId}/{service}/data/{action}";

        private string GetPublishTopic(string service, string action) => $"/remoteapp/tv/{service}/{this._options.ClientId}/actions/{action}";

        private Task OnDisconnected(MqttClientDisconnectedEventArgs e)
        {
            logger.LogInformation("Disconnected: {reason}", e.Reason);
            this.Disconnected?.Invoke(this, e);
            return Task.CompletedTask;
        }

        private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            string topic = e.ApplicationMessage.Topic;
            string payload = e.ApplicationMessage.Payload is { } bytes ? Encoding.UTF8.GetString(bytes) : string.Empty;
            logger.LogInformation("Received message '{payload}' to topic '{topic}'.", payload, topic);
            this.MessageReceived?.Invoke(this, new((topic, payload)));
            switch (topic)
            {
                case BroadcastTopics.Sleep:
                    this.Sleep?.Invoke(this, EventArgs.Empty);
                    break;

                case BroadcastTopics.VolumeChange:
                    this.VolumeChanged?.Invoke(this, new(Connection.TranslateVolumePayload(payload)));
                    break;

                case BroadcastTopics.Launcher:
                case BroadcastTopics.Settings:
                case BroadcastTopics.State:
                    this.StateChanged?.Invoke(this, new(this.TranslateStateMessage(topic, payload)));
                    break;
            }
            return e.AcknowledgeAsync(default);
        }

        private IState TranslateStateMessage(string topic, string payload)
        {
            switch (topic)
            {
                case BroadcastTopics.Launcher:
                    return new State(StateType.Launcher);

                case BroadcastTopics.Settings:
                    return new State(StateType.Settings);

                case BroadcastTopics.State:
                    StateData data = JsonSerializer.Deserialize<StateData>(payload, Connection._jsonOptions);
                    return data.Type switch
                    {
                        "livetv" => new State(StateType.LiveTV),
                        "sourceswitch" => new State(StateType.SourceSwitch),
                        "app" => new AppState(new(data.Name!, data.Url!)),
                        _ => throw new ApplicationException($"Unexpected payload: {payload}"),
                    };
            }
            return new State(topic == this.GetDataTopic("ui_service", "authentication") ? StateType.AuthenticationRequired : StateType.Unknown);
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

    private readonly record struct State(StateType Type) : IState;

    private readonly record struct SuccessPayload(int Result);

    private readonly record struct VolumeData(
        [property: JsonPropertyName("volume_type")] int Type,
        [property: JsonPropertyName("volume_value")] int Value
    );

    private readonly record struct StateData(
        [property: JsonPropertyName("statetype")] string Type,
        string? Name = default,
        string? Url = default
    );
}
