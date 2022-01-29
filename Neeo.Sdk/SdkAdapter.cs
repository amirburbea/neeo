using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Neeo.Sdk;

public sealed class SdkAdapter : IAsyncDisposable, IDisposable
{
    private IHost? _host;

    public SdkAdapter(ISdkEnvironment environment, IHost host) => (this.Environment, this._host) = (environment, host);

    public ISdkEnvironment Environment { get; }

    public void Dispose() => this.DisposeAsync().AsTask().Wait();

    public async ValueTask DisposeAsync()
    {
        using IHost? host = Interlocked.Exchange(ref this._host, null);
        if (host != null)
        {
            await host.StopAsync().ConfigureAwait(false);
        }
    }
}