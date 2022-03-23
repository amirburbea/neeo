using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Drivers.WebOS;

public static class DeviceDiscovery
{
    public static async IAsyncEnumerable<IPAddress> DiscoverTVsAsync(int scanTime = 5000, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(scanTime);
        using UdpClient client = new();
        HashSet<IPAddress> addresses = new();
        byte[] bytes = Encoding.UTF8.GetBytes(string.Format(Constants.SearchRequestTemplate, Constants.SecondScreenService));
        IPEndPoint endPoint = new(IPAddress.Parse("239.255.255.250"), 1900);
        while (!source.IsCancellationRequested)
        {
            UdpReceiveResult receiveResult;
            try
            {
                await client.SendAsync(bytes.AsMemory(), endPoint, cancellationToken).ConfigureAwait(false);
                receiveResult = await client.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
            if (!addresses.Add(receiveResult.RemoteEndPoint.Address))
            {
                continue;
            }
            using MemoryStream stream = new(receiveResult.Buffer);
            using StreamReader reader = new(stream);
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                if (line.Contains(Constants.SecondScreenService))
                {
                    yield return receiveResult.RemoteEndPoint.Address;
                    break;
                }
            }
        }
    }

    public static async Task<IPAddress?> DiscoverTVAsync(int scanTime = 5000, CancellationToken cancellationToken = default)
    {
        await foreach (IPAddress address in DeviceDiscovery.DiscoverTVsAsync(scanTime, cancellationToken).ConfigureAwait(false))
        {
            return address;
        }
        return default;
    }

    private static class Constants
    {
        public const string SearchRequestTemplate = @"M-SEARCH * HTTP/1.1
HOST: 239.255.255.250:1900
MAN: ""ssdp:discover""
MX: 5
ST: {0}
USER-AGENT: iOS/5.0 UDAP/2.0 iPhone/4

";

        public const string SecondScreenService = "urn:lge-com:service:webos-second-screen:1";
    }
}
