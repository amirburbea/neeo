using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Drivers.PlexApi;

internal class PlexClient(PlexPlayerInfo info, IPlexTokenStore tokenStore)
{
    private readonly Lock _lock = new();
    private int _commandId;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        string url = "ws;
    }
}
