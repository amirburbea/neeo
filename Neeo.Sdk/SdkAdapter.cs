using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Neeo.Sdk;
/*
public interface ISdkAdapter : IDisposable, IAsyncDisposable
{
    bool IsDisposed { get; }
}

internal sealed class SdkAdapter : ISdkAdapter
{


    public SdkAdapter()
    {
    }

    public bool IsDisposed => this._host != null;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref this._host, default) is not { } host)
        {
            return;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref this._host, default) is not { } host)
        {
            return;
        }
    }
}
*/