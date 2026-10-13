using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Drivers.Plex;

internal static partial class PlexDirectConnect
{
    /// <summary>
    /// Creates a <see cref="SocketsHttpHandler"/> with the configuration necessary to support SSL
    /// connections to Plex direct.
    /// <para/>
    /// Plex direct connect uses a fake hostname <c>$"{serverIPAddress.Replace('.', '-')}.{machineIdentifier}.plex.direct"</c>.
    /// <para/>
    /// When such a host name is encountered, it needs to be translated to the server IP address,
    /// and forgive <see cref="SslPolicyErrors.RemoteCertificateNameMismatch"/>.
    /// </summary>
    public static SocketsHttpHandler CreateHttpHandler() => new()
    {
        AutomaticDecompression = DecompressionMethods.All,
        ConnectCallback = PlexDirectConnect.ConnectSocketAsync,
        SslOptions = new() { RemoteCertificateValidationCallback = PlexDirectConnect.ValidateRemoteCertificate }
    };

    private static async ValueTask<Stream> ConnectSocketAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        if (IPAddress.TryParse(context.DnsEndPoint.Host, out IPAddress? ipAddress))
        {
            return await ConnectAsync(ipAddress).ConfigureAwait(false);
        }
        if (PlexDirectConnect.PlexDirectRegex().Match(context.DnsEndPoint.Host) is { Success: true, Groups: { } groups })
        {
            return await ConnectAsync(IPAddress.Parse(groups["ip"].Value.Replace('-', '.'))).ConfigureAwait(false);
        }
        IPHostEntry entry = await Dns.GetHostEntryAsync(context.DnsEndPoint.Host, cancellationToken).ConfigureAwait(false);
        foreach (IPAddress address in entry.AddressList)
        {
            try
            {
                return await ConnectAsync(address).ConfigureAwait(false);
            }
            catch
            {
            }
        }
        throw new SocketException(11001); // Host not found.

        async ValueTask<Stream> ConnectAsync(IPAddress address)
        {
            Socket socket = new(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            try
            {
                await socket.ConnectAsync(address, context.DnsEndPoint.Port, cancellationToken).ConfigureAwait(false);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }
    }

    [GeneratedRegex(@"^(?<ip>(\d+[-]){3}\d+)\..+\.plex\.direct$", RegexOptions.ExplicitCapture | RegexOptions.Compiled)]
    private static partial Regex PlexDirectRegex();

    private static bool ValidateRemoteCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors errors) => errors switch
    {
        SslPolicyErrors.None => true,
        SslPolicyErrors.RemoteCertificateNameMismatch => sender is SslStream { TargetHostName: { } name } && PlexDirectConnect.PlexDirectRegex().IsMatch(name),
        _ => false,
    };
}
