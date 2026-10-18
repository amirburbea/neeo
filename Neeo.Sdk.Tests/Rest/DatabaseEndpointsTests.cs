using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Rest.Endpoints;
using Xunit;

namespace Neeo.Sdk.Tests.Rest;

public sealed class DatabaseEndpointsTests
{
    private readonly Mock<IDeviceDatabase> _mockDatabase = new(MockBehavior.Strict);

    [Fact]
    public async Task GetDeviceByAdapterName_should_respond_with_the_device()
    {
        DeviceModel model = DatabaseEndpointsTests.CreateModel(7);
        this._mockDatabase.Setup(database => database.GetDeviceByAdapterName(model.AdapterName)).Returns(model);
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/db/adapterdefinition/{model.AdapterName}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await DatabaseEndpointsTests.ReadJsonAsync(response);
        Assert.Equal(model.AdapterName, json.RootElement.GetProperty("adapterName").GetString());
    }

    [Fact]
    public async Task GetDeviceByAdapterName_should_respond_not_found_for_an_unknown_adapter()
    {
        this._mockDatabase.Setup(database => database.GetDeviceByAdapterName("unknown")).Returns((DeviceModel?)null);
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync("/db/adapterdefinition/unknown", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDeviceById_should_respond_with_the_device()
    {
        DeviceModel model = DatabaseEndpointsTests.CreateModel(7);
        this._mockDatabase.Setup(database => database.GetDeviceById(7)).Returns(model);
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync("/db/7", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await DatabaseEndpointsTests.ReadJsonAsync(response);
        Assert.Equal(7, json.RootElement.GetProperty("id").GetInt32());
    }

    [Fact]
    public async Task GetDeviceById_should_respond_not_found_for_an_unknown_id()
    {
        this._mockDatabase.Setup(database => database.GetDeviceById(8)).Returns((DeviceModel?)null);
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync("/db/8", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDeviceById_should_respond_bad_request_for_a_non_numeric_id()
    {
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync("/db/abc", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Search_should_pass_the_q_parameter_and_respond_with_the_results()
    {
        DeviceModel model = DatabaseEndpointsTests.CreateModel(7);
        this._mockDatabase.Setup(database => database.Search("light")).Returns([new DeviceSearchResult(model, 1.5, 2.0)]);
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync("/db/search?q=light", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = await DatabaseEndpointsTests.ReadJsonAsync(response);
        JsonElement result = Assert.Single(json.RootElement.EnumerateArray());
        Assert.Equal(1.5, result.GetProperty("score").GetDouble());
        Assert.Equal(2.0, result.GetProperty("maxScore").GetDouble());
    }

    [Fact]
    public async Task Search_without_a_query_should_pass_null()
    {
        this._mockDatabase.Setup(database => database.Search(null)).Returns([]);
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync("/db/search", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        this._mockDatabase.Verify(database => database.Search(null), Times.Once);
    }

    private static DeviceModel CreateModel(int id) => new(
        id,
        Device.Create("Test Device", DeviceType.Accessory)
            .AddTextLabel("TXT", "Text", _ => "x")
            .BuildAdapter()
    );

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

    private Task<EndpointTestHost> StartAsync() => EndpointTestHost.StartAsync(
        services => services.AddSingleton(this._mockDatabase.Object),
        endpoints => endpoints.MapDatabaseEndpoints(),
        TestContext.Current.CancellationToken
    );
}
