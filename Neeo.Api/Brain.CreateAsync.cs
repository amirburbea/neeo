using System;
using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Api;

partial class Brain
{
    public static async Task<Brain> CreateAsync(IPAddress ipAddress, int servicePort = 3000, CancellationToken cancellationToken = default)
    {
        if ((ipAddress ?? throw new ArgumentNullException(nameof(ipAddress))).AddressFamily != AddressFamily.InterNetwork)
        {
            throw new ArgumentException("Must be IPv4 address.", nameof(ipAddress));
        }
        using ApiClient client = new(new(ipAddress, servicePort));
        (string firmwareVersion, string hostName, string hardwareRegion) = await client.GetAsync<SystemInformation>(UrlPaths.SystemInformation, cancellationToken: cancellationToken).ConfigureAwait(false);
        return new(ipAddress, servicePort, hostName, hostName, firmwareVersion, hardwareRegion);
    }

    private sealed record class SystemInformation(String FirmwareVersion, [property: JsonPropertyName("hostname")] String HostName, String HardwareRegion);
}