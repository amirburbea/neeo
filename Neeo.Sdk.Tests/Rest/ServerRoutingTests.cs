using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Rest;
using Xunit;

namespace Neeo.Sdk.Tests.Rest;

/// <summary>
/// Exercises the real server pipeline, verifying that overlapping device routes resolve to the intended endpoint.
/// </summary>
public sealed class ServerRoutingTests
{
    private readonly Mock<IDeviceAdapter> _mockAdapter = new();
    private readonly Mock<IDeviceDatabase> _mockDatabase = new(MockBehavior.Strict);
    private readonly Mock<IPgpEncryption> _mockPgp = new(MockBehavior.Strict);

    public ServerRoutingTests()
    {
        this._mockAdapter.Setup(adapter => adapter.AdapterName).Returns(Constants.AdapterName);
        this._mockAdapter.Setup(adapter => adapter.DeviceName).Returns(Constants.AdapterName);
        this._mockAdapter.Setup(adapter => adapter.RouteHandler).Returns((request, path, _) => Task.FromResult(Results.Text($"custom:{path}")));
        this._mockPgp.Setup(pgp => pgp.PublicKeyText).Returns("KEY");
        this._mockDatabase
            .Setup(database => database.GetAdapterAsync(Constants.AdapterName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(this._mockAdapter.Object);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("x/y")]
    [InlineData("x/y/z")]
    public async Task Custom_route_wins_over_the_device_action_endpoints(string path)
    {
        using IHost host = await this.StartAsync();
        using HttpClient client = ServerRoutingTests.CreateClient(host);

        using HttpResponseMessage response = await client.GetAsync($"/device/{Constants.AdapterName}/custom/{path}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal($"custom:{path}", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Device_action_endpoints_are_still_reachable_next_to_the_custom_route()
    {
        Mock<IValueFeature> feature = new();
        feature.Setup(f => f.GetValueAsync("device-1", It.IsAny<CancellationToken>())).ReturnsAsync(new ValueResponse(7));
        this._mockAdapter.Setup(adapter => adapter.GetFeature("component")).Returns(feature.Object);
        using IHost host = await this.StartAsync();
        using HttpClient client = ServerRoutingTests.CreateClient(host);

        using HttpResponseMessage response = await client.GetAsync($"/device/{Constants.AdapterName}/component/device-1", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"value\":7", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Secure_endpoint_is_reachable_through_the_real_pipeline()
    {
        using IHost host = await this.StartAsync();
        using HttpClient client = ServerRoutingTests.CreateClient(host);

        using HttpResponseMessage response = await client.GetAsync("/secure/pubkey", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("""{"publickey":"KEY"}""", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private static HttpClient CreateClient(IHost host) => new()
    {
        BaseAddress = new(host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First())
    };

    private async Task<IHost> StartAsync()
    {
        IHost host = new HostBuilder()
            .ConfigureWebHostDefaults(builder => Server.ConfigureWebHostDefaults(builder, IPAddress.Loopback, 0))
            .ConfigureServices(services =>
            {
                services
                    .AddSingleton(this._mockDatabase.Object)
                    .AddSingleton(this._mockPgp.Object)
                    .AddSingleton(Mock.Of<IDynamicDeviceRegistry>());
            })
            .Build();
        await host.StartAsync(TestContext.Current.CancellationToken);
        return host;
    }

    private static class Constants
    {
        public const string AdapterName = "adapter";
    }
}
