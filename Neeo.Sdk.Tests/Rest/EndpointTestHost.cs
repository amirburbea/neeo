using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Neeo.Sdk.Rest;

namespace Neeo.Sdk.Tests.Rest;

/// <summary>
/// Hosts minimal API endpoints on a loopback port, configured with the same JSON options as the real server.
/// </summary>
internal sealed class EndpointTestHost : IAsyncDisposable
{
    private readonly WebApplication _app;

    private EndpointTestHost(WebApplication app)
    {
        this._app = app;
        this.Client = new() { BaseAddress = new(app.Urls.First()) };
    }

    public HttpClient Client { get; }

    public static async Task<EndpointTestHost> StartAsync(
        Action<IServiceCollection> configureServices,
        Action<IEndpointRouteBuilder> mapEndpoints,
        CancellationToken cancellationToken
    )
    {
        WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.ConfigureHttpJsonOptions(options => Server.ConfigureJsonOptions(options.SerializerOptions));
        configureServices(builder.Services);
        WebApplication app = builder.Build();
        mapEndpoints(app);
        await app.StartAsync(cancellationToken);
        return new(app);
    }

    public async ValueTask DisposeAsync()
    {
        this.Client.Dispose();
        await this._app.DisposeAsync();
    }
}
