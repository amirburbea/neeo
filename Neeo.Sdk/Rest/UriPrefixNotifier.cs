using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Rest;

internal sealed class UriPrefixNotifier : IHostedService
{
    private readonly IDeviceDatabase _database;
    private readonly ISdkEnvironment _environment;

    public UriPrefixNotifier(IDeviceDatabase database, ISdkEnvironment environment)
    {
        (this._database, this._environment) = (database, environment);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (IDeviceAdapter adapter in this._database.GetAdaptersWithDeviceRoutes())
        {
            adapter.UriPrefixCallback!($"{this._environment.HostAddress}/device/{adapter.AdapterName}/custom/");
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}