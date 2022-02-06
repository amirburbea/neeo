using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Hisense;

public sealed class HisenseClient : IDisposable
{
    private static readonly IMqttClientFactory _clientFactory = new MqttFactory();
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ConcurrentDictionary<string, Channel<MqttApplicationMessage>> _channels = new();
    private readonly IMqttClient _client;
    private readonly IMqttClientOptions _options;

    public HisenseClient(IPAddress ipAddress, PhysicalAddress macAddress, string? clientId = default)
    {
        this.IpAddress = ipAddress;
        this.MacAddress = macAddress;
        this.ClientId = clientId ?? $"{nameof(HisenseClient)}-{Guid.NewGuid()}";
        this._client = HisenseClient._clientFactory.CreateMqttClient();
        this._options = new MqttClientOptionsBuilder()
            .WithClientId(Uri.EscapeDataString(this.ClientId))
            .WithTcpServer(ipAddress.ToString(), 36669)
            .WithCredentials("hisenseservice", "multimqttservice")
            .WithTls(parameters: new() { UseTls = true, AllowUntrustedCertificates = true, IgnoreCertificateChainErrors = true, IgnoreCertificateRevocationErrors = true })
            .Build();
    }

    public string ClientId { get; }

    public IPAddress IpAddress { get; }

    public PhysicalAddress MacAddress { get; }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken)
    {
        if (await this._client.ConnectAsync(this._options, cancellationToken).ConfigureAwait(false) is not { ResultCode: MqttClientConnectResultCode.Success })
        {
            return false;
        }
        this._client.UseApplicationMessageReceivedHandler(this.OnMessageReceived);
        await this._client.SubscribeAsync("/remoteapp/mobile/#");
        return true;
    }

    public void Dispose()
    {
        foreach (Channel<MqttApplicationMessage> channel in this._channels.Values)
        {
            channel.Writer.Complete();
        }
        this._client.Dispose();
    }

    public Task RequestLogin()
    {
        return this.SendMessageAsync("ui_service", "gettvstate");
    }

    public async Task SendMessageAsync(string service, string action, object? body = default)
    {
        string publishTopic = this.GetPublishTopic(service, action);
        string payload = body is null ? "{}" : JsonSerializer.Serialize(body, HisenseClient._jsonOptions);
        Console.WriteLine("sending message to " + publishTopic);
        await this._client.PublishAsync(publishTopic, payload).ConfigureAwait(false);
    }

    public async Task TryLogin(string code)
    {
        await this.SendMessageAsync("ui_service", "authenticationcode", new AuthenticationPayload(code));
        string subscribeTopic = this.GetSubscribeTopic("ui_service", "data/authenticationcode");

        do
        {
            var message = await WaitForMessage(subscribeTopic);
            if (message.Payload is { Length: > 0 })
            {
                return;
            }
        }
        while (true);
    }

    public Task WakeAsync() => WakeOnLan.WakeAsync(this.MacAddress);

    private Channel<MqttApplicationMessage> GetChannel(string topic) => this._channels.GetOrAdd(topic, _ => Channel.CreateUnbounded<MqttApplicationMessage>());

    private string GetPublishTopic(string service, string action) => $"/remoteapp/tv/{service}/{this.ClientId}/actions/{action}";

    private string GetSubscribeTopic(string service, string suffix) => $"/remoteapp/mobile/{this.ClientId}/{service}/{suffix}";

    private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        Console.WriteLine("Message came in " + e.ApplicationMessage.Topic);
        await this.GetChannel(e.ApplicationMessage.Topic).Writer.WriteAsync(e.ApplicationMessage).ConfigureAwait(false);
        if (e.ApplicationMessage.Payload is { Length: > 0 } payload)
        {
            Console.WriteLine(Encoding.UTF8.GetString(payload));
        }
        else if (e.ProcessingFailed)
        {
            Console.WriteLine("Processing failed.");
        }
        await e.AcknowledgeAsync(default);
    }

    private async Task<MqttApplicationMessage> WaitForMessage(string topic)
    {
        return await this.GetChannel(topic).Reader.ReadAsync().ConfigureAwait(false);
    }

    public record AuthenticationPayload(string AuthNum);
}