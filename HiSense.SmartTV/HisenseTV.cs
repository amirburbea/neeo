using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Exceptions;

namespace HiSense.SmartTV;

public class HisenseTV
{


    private static readonly MqttFactory _mqttFactory = new();

    public static async Task<IMqttClient?> DiscoverAsync()
    {
        using CancellationTokenSource cts = new();
        TaskCompletionSource<IMqttClient?> taskCompletionSource = new();
        try
        {
            await Task.WhenAll(GetAddresses().Select(async address =>
            {
                if (await ConnectAsync(address, cts.Token).ConfigureAwait(false) is not { } client)
                {
                    return;
                }
                taskCompletionSource.TrySetResult(client);
                cts.Cancel();
            })).ConfigureAwait(false);
            taskCompletionSource.TrySetResult(null);
        }
        catch (OperationCanceledException)
        {
        }
        return await taskCompletionSource.Task.ConfigureAwait(false);

        async Task<IMqttClient> ConnectAsync(IPAddress address, CancellationToken cancellationToken)
        {
            IMqttClient? client = default;
            try
            {
                if (await TryPingAsync(address).ConfigureAwait(false))
                {
                    client = HisenseTV._mqttFactory.CreateMqttClient();
                    if (await client.ConnectAsync(HisenseTV.CreateClientOptions(address), cancellationToken).ConfigureAwait(false) is { ResultCode: MqttClientConnectResultCode.Success })
                    {
                        return client;
                    }
                }
            }
            catch (MqttCommunicationException)
            {
                // Pinged something but not a Hisense TV.
                client?.Dispose();
            }
            catch (OperationCanceledException)
            {
                // Cancelled because we found a Hisense TV.
                client?.Dispose();
            }
            return default;
        }

        static IEnumerable<IPAddress> GetAddresses()
        {
            foreach (IPAddress address in Dns.GetHostAddresses(Dns.GetHostName(), AddressFamily.InterNetwork))
            {
                if (IPAddress.IsLoopback(address) || IPHelper.IsMulticast(address))
                {
                    continue;
                }
                byte[] bytes = address.GetAddressBytes();
                for (byte i = 1; i < 255; i++)
                {
                    if (i == bytes[3])
                    {
                        continue;
                    }
                    byte[] other = new byte[4];
                    Buffer.BlockCopy(bytes, 0, other, 0, 3);
                    other[3] = (byte)i;
                    yield return new(other);
                }
            }
        }

        async Task<bool> TryPingAsync(IPAddress address)
        {
            using Ping ping = new();
            cts.Token.Register(ping.SendAsyncCancel);
            TaskCompletionSource<bool> taskSource = new();
            ping.PingCompleted += OnPingCompleted;
            ping.SendAsync(address, 15, taskSource);
            bool success = await taskSource.Task.ConfigureAwait(false);
            ping.PingCompleted -= OnPingCompleted;
            return success;

            static void OnPingCompleted(object? _, PingCompletedEventArgs e) => ((TaskCompletionSource<bool>)e.UserState!).TrySetResult(e.Reply?.Status == IPStatus.Success);
        }
    }



    private static IMqttClientOptions CreateClientOptions(IPAddress ipAddress) => new MqttClientOptionsBuilder()
        .WithTcpServer(ipAddress.ToString(), 36669)
        .WithCredentials("hisenseservice", "multimqttservice")
        .WithTls(parameters: new() { UseTls = true, AllowUntrustedCertificates = true, IgnoreCertificateChainErrors = true, IgnoreCertificateRevocationErrors = true })
        .Build();
}

internal class Foo
{
    private static async Task Main()
    {
        await WakeOnLan.WakeAsync("18:30:0C:C3:F4:C8");
        //var dict = IPHelper.GetAllDevicesOnLAN();

        if (await HisenseTV.DiscoverAsync() is not { } client)
        {
            return;
        }
            await client.PublishAsync(new MqttApplicationMessage() { Topic = "ui.service", Payload = Encoding.UTF8.GetBytes("gettvstate") });
            var r = await client.SubscribeAsync(new MQTTnet.Client.Subscribing.MqttClientSubscribeOptions()
            {
                TopicFilters = { new() { Topic = "ui.service" } }
            });
        

        //
      }
}