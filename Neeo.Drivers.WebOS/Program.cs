using System;
using System.Net;
using System.Threading.Tasks;

namespace Neeo.Drivers.WebOS;

static class Program
{
    static async Task Main()
    {
        await foreach (IPAddress item in DeviceDiscovery.DiscoverTVsAsync())
        {
            Console.WriteLine(item);
        }
    }
}