using System;
using System.Collections.Concurrent;
using System.Diagnostics;
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

public enum StateType
{
    AuthenticationRequired,
    Launcher,
    LiveTV,
    App
}

public interface IState
{
    StateType Type { get; }
}

public class HisenseTV : IDisposable
{
    public static string? ClientIdSuffix;

    private Connection? _connection;

    private HisenseTV(IPAddress ipAddress, PhysicalAddress macAddress)
    {
        this.IPAddress = ipAddress;
        this.MacAddress = macAddress;
    }

    public IPAddress IPAddress { get; }

    public bool IsConnected => this._connection != null && this._connection.IsConnected;

    public PhysicalAddress MacAddress { get; }

    public static async Task<HisenseTV[]> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        ConcurrentBag<HisenseTV> bag = new();
        await Parallel.ForEachAsync(
            NetworkDevices.GetNetworkDevices(),
            cancellationToken,
            async (pair, cancellationToken) =>
            {
                (IPAddress ipAddress, PhysicalAddress macAddress) = pair;
                try
                {
                    if (await HisenseTV.TryCreate(ipAddress, macAddress, true, cancellationToken).ConfigureAwait(false) is not { } tv)
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

    public static Task<HisenseTV?> DiscoverOneAsync(CancellationToken cancellationToken = default)
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
                    NetworkDevices.GetNetworkDevices(),
                    cts.Token,
                    async (pair, cancellationToken) =>
                    {
                        (IPAddress ipAddress, PhysicalAddress macAddress) = pair;
                        try
                        {
                            if (await HisenseTV.TryCreate(ipAddress, macAddress, true, cancellationToken).ConfigureAwait(false) is not { } tv)
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

    public static async Task<HisenseTV?> TryCreate(PhysicalAddress macAddress, bool connectionRequired = false, CancellationToken cancellationToken = default)
    {
        foreach ((IPAddress ipAddress, PhysicalAddress physicalAddress) in NetworkDevices.GetNetworkDevices())
        {
            if (macAddress.Equals(physicalAddress))
            {
                return await HisenseTV.TryCreate(ipAddress, macAddress, connectionRequired, cancellationToken).ConfigureAwait(false);
            }
        }
        return default;
    }

    public void Dispose() => this._connection?.Dispose();

    [Obsolete("DELETE AND MAKE CLASS PRIVATE")]
    public Connection? GetConnection() => this._connection;

    private static async Task<HisenseTV?> TryCreate(IPAddress ipAddress, PhysicalAddress macAddress, bool connectionRequired = false, CancellationToken cancellationToken = default)
    {
        if (!await HisenseTV.TryPingAsync(ipAddress, cancellationToken).ConfigureAwait(false))
        {
            // Even when off, the device should have responded to a Ping.
            return default;
        }
        HisenseTV tv = new(ipAddress, macAddress);
        // Try to connect even if not required.
        if (await tv.TryConnectAsync(cancellationToken).ConfigureAwait(false) || !connectionRequired)
        {
            return tv;
        }
        tv.Dispose();
        return default;
    }

    private static async Task<bool> TryPingAsync(IPAddress address, CancellationToken cancellationToken)
    {
        using Ping ping = new();
        TaskCompletionSource<bool> taskSource = new();
        await using (cancellationToken.Register(OnCancellationRequested).ConfigureAwait(false))
        {
            ping.PingCompleted += OnPingCompleted;
            ping.SendAsync(address, 25, taskSource);
            bool success = await taskSource.Task.ConfigureAwait(false);
            ping.PingCompleted -= OnPingCompleted;
            return success;

            static void OnPingCompleted(object? _, PingCompletedEventArgs e) => ((TaskCompletionSource<bool>)e.UserState!).TrySetResult(e.Reply is { Status: IPStatus.Success });
        }

        void OnCancellationRequested()
        {
            ping.SendAsyncCancel();
            taskSource.TrySetCanceled(cancellationToken);
        }
    }

    private async Task<bool> TryConnectAsync(CancellationToken cancellationToken)
    {
        Connection connection = new(this.IPAddress, this.MacAddress);
        if (await connection.TryConnectAsync(cancellationToken).ConfigureAwait(false))
        {
            this._connection = connection;
            return true;
        }
        connection.Dispose();
        return false;
    }

    public sealed class Connection : IDisposable
    {
        private static readonly string _broadcastLauncherTopic = Connection.GetBroadcastTopic("ui_service", "actions/remote_launcher");
        private static readonly string _broadcastStateTopic = GetBroadcastTopic("ui_service", suffix: "state");
        private static readonly string _broadcastVolumeChangeTopic = GetBroadcastTopic("platform_service", suffix: "actions/volumechange");
        private static readonly IMqttClientFactory _clientFactory = new MqttFactory();
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        private readonly ConcurrentDictionary<string, Action<MqttApplicationMessage>> _actions = new();
        private readonly IMqttClient _client;
        private readonly string _clientId;
        private readonly IMqttClientOptions _options;

        public Connection(IPAddress ipAddress, PhysicalAddress macAddress)
        {
            this._client = Connection._clientFactory.CreateMqttClient();
            this._clientId = Uri.EscapeDataString($"{macAddress}_{(ClientIdSuffix ?? Dns.GetHostName())}");
            this._options = new MqttClientOptionsBuilder()
                .WithClientId(this._clientId)
                .WithTcpServer(ipAddress.ToString(), 36669)
                .WithCredentials("hisenseservice", "multimqttservice")
                .WithTls(parameters: new() { UseTls = true, AllowUntrustedCertificates = true, IgnoreCertificateChainErrors = true, IgnoreCertificateRevocationErrors = true })
                .Build();
        }

        public event EventHandler? Disconnected;

        public event EventHandler<StateChangedEventArgs>? StateChanged;

        public event EventHandler<VolumeChangedEventArgs>? VolumeChanged;

        public bool IsConnected => this._client.IsConnected;

        public async Task<IState> AuthenticateAsync(string code, CancellationToken cancellationToken = default)
        {
            const string action = "authenticationcode";
            const string service = "ui_service";
            await this.SendMessageAsync(service, action, new AuthenticationNumber(code)).ConfigureAwait(false);
            MqttApplicationMessage message = await this.WaitForMessageAsync(this.GetDataTopic(service, action), cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<SuccessPayload>(message.Payload.AsSpan(), Connection._jsonOptions).Result != 1
                ? new GenericState(StateType.AuthenticationRequired)
                : await this.GetStateAsync(cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            this._client.Dispose();
        }

        public async Task<AppInfo[]> GetAppsAsync(CancellationToken cancellationToken = default)
        {
            const string service = "ui_service";
            const string action = "applist";
            await this.SendMessageAsync(service, action).ConfigureAwait(false);
            MqttApplicationMessage message = await this.WaitForMessageAsync(this.GetDataTopic(service, action), cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<AppInfo[]>(message.Payload, Connection._jsonOptions)!;
        }

        public async Task<IState> GetStateAsync(CancellationToken cancellationToken = default)
        {
            const string service = "ui_service";
            await this.SendMessageAsync(service, "gettvstate").ConfigureAwait(false);

            string authenticationRequiredTopic = this.GetDataTopic(service, "authentication");
            MqttApplicationMessage message = await this.WaitForFirstMessageAsync(
                new[] { Connection._broadcastStateTopic, _broadcastLauncherTopic, authenticationRequiredTopic },
                cancellationToken
            ).ConfigureAwait(false);
            return this.TranslateStateMessage(message);
        }

        public async Task<int> GetVolumeAsync(CancellationToken cancellationToken = default)
        {
            const string action = "getvolume";
            await this.SendMessageAsync("ui_service", action).ConfigureAwait(false);
            MqttApplicationMessage message = await this.WaitForMessageAsync(this.GetDataTopic("platform_service", action), cancellationToken).ConfigureAwait(false);
            return Connection.TranslateVolumeMessage(message);
        }

        public async Task SendMessageAsync(string service, string action, object? body = default)
        {
            string publishTopic = this.GetPublishTopic(service, action);
            string payload = body is null ? "{}" : JsonSerializer.Serialize(body, Connection._jsonOptions);
            Debug.WriteLine("sending message '{0}' to topic {1}.", payload, publishTopic);
            await this._client.PublishAsync(publishTopic, payload).ConfigureAwait(false);
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

        private static int TranslateVolumeMessage(MqttApplicationMessage message)
        {
            VolumeData data = JsonSerializer.Deserialize<VolumeData>(message.Payload.AsSpan(), Connection._jsonOptions);
            return data.Value;
        }

        private string GetDataTopic(string service, string action) => $"/remoteapp/mobile/{this._clientId}/{service}/data/{action}";

        private string GetPublishTopic(string service, string action) => $"/remoteapp/tv/{service}/{this._clientId}/actions/{action}";

        private Task OnDisconnected(MqttClientDisconnectedEventArgs e)
        {
            Debug.WriteLine("Disconnected: {0}", Enum.GetName(e.Reason));
            this.Disconnected?.Invoke(this, e);
            return Task.CompletedTask;
        }

        private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            return Task.Factory.StartNew(delegate
            {
                Debug.WriteLine("Received message on topic {0}", (object)e.ApplicationMessage.Topic);
                string topic = e.ApplicationMessage.Topic;
                if (this._actions.TryRemove(topic, out Action<MqttApplicationMessage>? action))
                {
                    action(e.ApplicationMessage);
                }
                else if (topic == Connection._broadcastVolumeChangeTopic)
                {
                    this.VolumeChanged?.Invoke(this, new(Connection.TranslateVolumeMessage(e.ApplicationMessage)));
                }
                else if (topic == Connection._broadcastStateTopic || topic == Connection._broadcastLauncherTopic)
                {
                    this.StateChanged?.Invoke(this, new(this.TranslateStateMessage(e.ApplicationMessage)));
                }
            }).ContinueWith(_ => e.AcknowledgeAsync(default));
        }

        private IState TranslateStateMessage(MqttApplicationMessage message)
        {
            if (message.Topic == Connection._broadcastLauncherTopic)
            {
                return new GenericState(StateType.Launcher);
            }
            if (message.Topic == this.GetDataTopic("ui_service", "authentication"))
            {
                return new GenericState(StateType.AuthenticationRequired);
            }
            StateData data = JsonSerializer.Deserialize<StateData>(message.Payload, Connection._jsonOptions);
            return data.Type switch
            {
                "livetv" => new GenericState(StateType.LiveTV),
                "app" => new AppState(new(data.Name!, data.Url!)),
                _ => throw new(Encoding.UTF8.GetString(message.Payload)),
            };
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

        private async Task<MqttApplicationMessage> WaitForMessageAsync(string topic, CancellationToken cancellationToken = default)
        {
            TaskCompletionSource<MqttApplicationMessage> taskSource = new();
            await using (cancellationToken.Register(() => taskSource.TrySetCanceled(cancellationToken)).ConfigureAwait(false))
            {
                Action<MqttApplicationMessage> action = taskSource.SetResult;
                try
                {
                    if (!this._actions.TryAdd(topic, action))
                    {
                        throw new();
                    }
                    return await taskSource.Task.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    this._actions.TryRemove(new(topic, action));
                    throw;
                }
            }
        }
    }

    private record struct GenericState(StateType Type) : IState;

    private record AuthenticationNumber(string AuthNum);

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

public sealed class StateChangedEventArgs : EventArgs
{
    public StateChangedEventArgs(IState state) => this.State = state;

    public IState State { get; }
}

public sealed class VolumeChangedEventArgs : EventArgs
{
    public VolumeChangedEventArgs(int volume) => this.Volume = volume;

    public int Volume { get; }
}

public record struct AppState(AppInfo App) : IState
{
    StateType IState.Type => StateType.App;
};

public record struct AppInfo(string Name, string Url);