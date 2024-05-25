using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Rest;

/// <summary>
/// Notifies adapters supporting device routes of their associated URI prefix.
/// Requests beginning with such a prefix will be passed to the route handler to be processed.
/// </summary>
internal sealed class UriPrefixNotifier(IDeviceDatabase database, ISdkEnvironment environment) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Parallel.ForEach(database.Adapters, new() { CancellationToken = cancellationToken }, adapter =>
        {
            if (adapter.UriPrefixCallback is { } callback)
            {
                callback($"{environment.HostAddress}/device/{adapter.AdapterName}/custom/");
            }
        });
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
