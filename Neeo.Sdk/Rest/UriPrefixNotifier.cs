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

    public Task StartAsync(CancellationToken cancellationToken) => Parallel.ForEachAsync(this._database.Adapters, cancellationToken, async (adapter, _) =>
    {
        if (adapter.UriPrefixCallback is { } callback)
        {
            await callback($"{this._environment.HostAddress}/device/{adapter.AdapterName}/custom/").ConfigureAwait(false);
        }
    });

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}