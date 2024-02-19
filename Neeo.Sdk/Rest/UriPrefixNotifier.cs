using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Rest;

/// <summary>
/// Notifies adapters supporting device routes of their associated URI prefix.
/// Requests beginning with such a prefix will be passed to the route handler to be processed.
/// </summary>
internal sealed class UriPrefixNotifier : IHostedService
{
    private readonly IDeviceDatabase _database;
    private readonly ISdkEnvironment _environment;

    public UriPrefixNotifier(IDeviceDatabase database, ISdkEnvironment environment)
    {
        (this._database, this._environment) = (database, environment);
    }

    public Task StartAsync(CancellationToken cancellationToken = default) => Parallel.ForEachAsync(
        this._database.Adapters.Where(adapter => adapter.UriPrefixCallback != null),
        cancellationToken,
        (adapter, _) => adapter.UriPrefixCallback!($"{this._environment.HostAddress}/device/{adapter.AdapterName}/custom/")
    );

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
