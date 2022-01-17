using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Exceptions;

namespace HiSense.SmartTV;

public class HisenseTV
{
    public static async Task<HisenseTV> Discover()
    {
        static async IAsyncEnumerable<IPAddress> GetAddressesAsync()
        {
            foreach (var address in await Dns.GetHostAddressesAsync(Dns.GetHostName(), AddressFamily.InterNetwork).ConfigureAwait(false))
            {
                if (IPAddress.IsLoopback(address))
                {
                    continue;
                }
                byte[] bytes = address.GetAddressBytes();
                for (int i = 1; i < 255; i++)
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

        static async Task<bool> TryPing(IPAddress address, CancellationToken cancellationToken)
        {
            using Ping ping = new();
            cancellationToken.Register(ping.SendAsyncCancel);
            TaskCompletionSource<bool> taskSource = new();

            static void OnPingCompleted(object? sender, PingCompletedEventArgs e)
            {
                if (e.UserState is TaskCompletionSource<bool> taskSource)
                {
                    taskSource.TrySetResult(e.Reply?.Status == IPStatus.Success);
                }
            }

            ping.PingCompleted += OnPingCompleted;
            ping.SendAsync(address, 15, taskSource);
            bool success = await taskSource.Task.ConfigureAwait(false);
            ping.PingCompleted -= OnPingCompleted;
            return success;
        }


        Stopwatch watch = Stopwatch.StartNew();
        IPAddress? ipAddress = default;
        using CancellationTokenSource source = new();
        MqttFactory factory = new();
        try
        {
            await Parallel.ForEachAsync(GetAddressesAsync(), new ParallelOptions { MaxDegreeOfParallelism = 48, CancellationToken = source.Token }, async (address, cancellationToken) =>
             {
                 if (!await TryPing(address, cancellationToken).ConfigureAwait(false))
                 {
                     return;
                 }
                 using var client = factory.CreateMqttClient();
                 try
                 {
                     var result = await client.ConnectAsync(new MqttClientOptionsBuilder()
                         .WithTcpServer(address.ToString(), 36669)
                         .WithCredentials("hisenseservice", "multimqttservice")
                         .WithTls(parameters: new() { UseTls = true, AllowUntrustedCertificates = true, IgnoreCertificateChainErrors = true, IgnoreCertificateRevocationErrors = true })
                         .Build(),
                         cancellationToken
                     ).ConfigureAwait(false);
                     ipAddress = address;
                     source.Cancel();
                 }
                 catch (MqttCommunicationException)
                 {
                 }
                 catch (OperationCanceledException)
                 {
                 }
             });
        }
        catch (OperationCanceledException)
        {
        }

        watch.Stop();
        throw new($"[{ipAddress}]:{watch.Elapsed.TotalMilliseconds}");
    }


}

internal class Foo
{
    private static async Task Main()
    {
        var dict = IPHelper.GetAllDevicesOnLAN();

        //await HisenseTV.Discover();

        await WakeOnLan.WakeAsync("18:30:0c:c3:f4:c8");


        
    }
}
