using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Remote.Neeo;

partial class Brain
{
    public static async Task<Brain> CreateAsync(IPAddress ipAddress, int servicePort = 3000, CancellationToken cancellationToken = default)
    {
        if ((ipAddress ?? throw new ArgumentNullException(nameof(ipAddress))).AddressFamily != AddressFamily.InterNetwork)
        {
            throw new ArgumentException("Must be IPv4 address.", nameof(ipAddress));
        }
        string uri = $"http://{ipAddress}:{servicePort}{UrlPaths.SystemInformation}";
        using HttpClient httpClient = new(HttpClientMethods.ClientHandler);
        SystemInformation sysInfo = await httpClient.FetchAsync<SystemInformation>(uri, HttpMethod.Get, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new(ipAddress, servicePort, sysInfo.HostName, sysInfo.HostName, sysInfo.FirmwareVersion, sysInfo.HardwareRegion);
    }

    private record class SystemInformation(String FirmwareVersion, [property: JsonPropertyName("hostname")] String HostName, String HardwareRegion);
}