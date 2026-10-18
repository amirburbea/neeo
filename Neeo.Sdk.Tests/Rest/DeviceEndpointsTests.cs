using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Features;
using Neeo.Sdk.Devices.Setup;
using Neeo.Sdk.Rest;
using Neeo.Sdk.Rest.Endpoints;
using Xunit;

namespace Neeo.Sdk.Tests.Rest;

public sealed class DeviceEndpointsTests
{
    private readonly Mock<IDeviceAdapter> _mockAdapter = new();
    private readonly Mock<IDeviceDatabase> _mockDatabase = new(MockBehavior.Strict);
    private readonly Mock<IDynamicDeviceRegistry> _mockDynamicRegistry = new(MockBehavior.Strict);
    private readonly Mock<IPgpEncryption> _mockPgp = new(MockBehavior.Strict);

    public DeviceEndpointsTests()
    {
        this._mockAdapter.Setup(adapter => adapter.AdapterName).Returns(Constants.AdapterName);
        this._mockAdapter.Setup(adapter => adapter.DeviceName).Returns(Constants.AdapterName);
        this._mockDatabase
            .Setup(database => database.GetAdapterAsync(Constants.AdapterName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(this._mockAdapter.Object);
        this._mockDatabase
            .Setup(database => database.GetAdapterAsync(Constants.UnknownAdapterName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDeviceAdapter?)null);
    }

    [Fact]
    public async Task QueryIsRegistered_should_respond_with_the_registration_state()
    {
        Mock<IRegistrationFeature> feature = this.SetupFeature<IRegistrationFeature>(ComponentType.Registration);
        feature.Setup(f => f.QueryIsRegisteredAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new IsRegisteredResponse(true));
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.AdapterName}/registered", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await DeviceEndpointsTests.ReadJsonAsync(response)).RootElement.GetProperty("registered").GetBoolean());
    }

    [Fact]
    public async Task QueryIsRegistered_should_respond_not_found_for_an_unknown_adapter()
    {
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.UnknownAdapterName}/registered", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Register_should_respond_bad_request_when_the_payload_cannot_be_decrypted()
    {
        this.SetupFeature<IRegistrationFeature>(ComponentType.Registration);
        this._mockPgp.Setup(pgp => pgp.DecryptAsync("bad", It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.PostAsJsonAsync($"/device/{Constants.AdapterName}/register", new { data = "bad" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_should_respond_bad_request_when_the_payload_has_no_data()
    {
        this.SetupFeature<IRegistrationFeature>(ComponentType.Registration);
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.PostAsJsonAsync($"/device/{Constants.AdapterName}/register", new { }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_should_respond_forbidden_with_the_error_text_when_registration_fails()
    {
        Mock<IRegistrationFeature> feature = this.SetupFeature<IRegistrationFeature>(ComponentType.Registration);
        byte[] decrypted = [1, 2, 3];
        this._mockPgp.Setup(pgp => pgp.DecryptAsync("good", It.IsAny<CancellationToken>())).ReturnsAsync(decrypted);
        feature.Setup(f => f.RegisterAsync(decrypted, It.IsAny<CancellationToken>())).ReturnsAsync(new RegistrationResult("nope"));
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.PostAsJsonAsync($"/device/{Constants.AdapterName}/register", new { data = "good" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        // The Brain relies on the explicit charset (as sent by the former MVC controller).
        Assert.Equal("text/plain; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        Assert.Equal("nope", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Register_should_respond_ok_when_registration_succeeds()
    {
        Mock<IRegistrationFeature> feature = this.SetupFeature<IRegistrationFeature>(ComponentType.Registration);
        byte[] decrypted = [1, 2, 3];
        this._mockPgp.Setup(pgp => pgp.DecryptAsync("good", It.IsAny<CancellationToken>())).ReturnsAsync(decrypted);
        feature.Setup(f => f.RegisterAsync(decrypted, It.IsAny<CancellationToken>())).ReturnsAsync(RegistrationResult.Success);
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.PostAsJsonAsync($"/device/{Constants.AdapterName}/register", new { data = "good" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Subscribe_should_notify_the_subscription_feature()
    {
        Mock<ISubscriptionFeature> feature = this.SetupFeature<ISubscriptionFeature>(ComponentType.Subscription);
        feature.Setup(f => f.NotifyDeviceAddedAsync(Constants.DeviceId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.AdapterName}/subscribe/{Constants.DeviceId}/ignored", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        feature.Verify(f => f.NotifyDeviceAddedAsync(Constants.DeviceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Subscribe_should_respond_ok_when_the_adapter_has_no_subscription_feature()
    {
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.AdapterName}/subscribe/{Constants.DeviceId}/ignored", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unsubscribe_should_notify_the_subscription_feature()
    {
        Mock<ISubscriptionFeature> feature = this.SetupFeature<ISubscriptionFeature>(ComponentType.Subscription);
        feature.Setup(f => f.NotifyDeviceRemovedAsync(Constants.DeviceId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.AdapterName}/unsubscribe/{Constants.DeviceId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        feature.Verify(f => f.NotifyDeviceRemovedAsync(Constants.DeviceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Subscribe_should_respond_not_found_for_an_unknown_adapter()
    {
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.UnknownAdapterName}/subscribe/{Constants.DeviceId}/ignored", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Discover_should_respond_with_the_discovered_devices()
    {
        Mock<IDiscoveryFeature> feature = this.SetupFeature<IDiscoveryFeature>(ComponentType.Discovery);
        feature.Setup(f => f.EnableDynamicDeviceBuilder).Returns(false);
        feature.Setup(f => f.DiscoverAsync(It.IsAny<CancellationToken>())).ReturnsAsync([new DiscoveredDevice("id-1", "Device 1", true, "Den")]);
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.AdapterName}/discover", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement device = Assert.Single((await DeviceEndpointsTests.ReadJsonAsync(response)).RootElement.EnumerateArray());
        Assert.Equal("id-1", device.GetProperty("id").GetString());
        Assert.Equal("Den", device.GetProperty("room").GetString());
        Assert.False(device.TryGetProperty("device", out _));
    }

    [Fact]
    public async Task Discover_with_dynamic_device_builder_should_respond_with_registered_dynamic_devices()
    {
        Mock<IDiscoveryFeature> feature = this.SetupFeature<IDiscoveryFeature>(ComponentType.Discovery);
        feature.Setup(f => f.EnableDynamicDeviceBuilder).Returns(true);
        IDeviceBuilder builder = Device.Create("Dynamic Light", DeviceType.Light).AddTextLabel("TXT", "Text", _ => "x");
        feature.Setup(f => f.DiscoverAsync(It.IsAny<CancellationToken>())).ReturnsAsync([new DiscoveredDevice("id-1", "Device 1", true, "Den", builder)]);
        IDeviceAdapter dynamicAdapter = builder.BuildAdapter();
        this._mockDynamicRegistry.Setup(registry => registry.RegisterDiscoveredDevice(this._mockAdapter.Object, "id-1", builder)).Returns(dynamicAdapter);
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.AdapterName}/discover", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement device = Assert.Single((await DeviceEndpointsTests.ReadJsonAsync(response)).RootElement.EnumerateArray());
        Assert.Equal("id-1", device.GetProperty("id").GetString());
        Assert.Equal(dynamicAdapter.AdapterName, device.GetProperty("device").GetProperty("adapterName").GetString());
        Assert.Equal("1.0", device.GetProperty("device").GetProperty("apiversion").GetString());
    }

    [Fact]
    public async Task Discover_should_respond_not_found_without_a_discovery_feature()
    {
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.AdapterName}/discover", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Execute_should_pass_the_favorite_to_the_favorites_feature()
    {
        Mock<IFavoritesFeature> feature = this.SetupNamedFeature<IFavoritesFeature>(Constants.ComponentName);
        feature.Setup(f => f.ExecuteAsync(Constants.DeviceId, "POWER_ON", It.IsAny<CancellationToken>())).ReturnsAsync(new SuccessResponse(true));
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.PostAsJsonAsync(
            $"/device/{Constants.AdapterName}/{Constants.ComponentName}/{Constants.DeviceId}",
            new { favoriteId = "POWER_ON" },
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await DeviceEndpointsTests.ReadJsonAsync(response)).RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task Execute_should_respond_not_found_for_a_component_that_is_neither_favorites_nor_directory()
    {
        this.SetupNamedFeature<IButtonFeature>(Constants.ComponentName);
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.PostAsJsonAsync(
            $"/device/{Constants.AdapterName}/{Constants.ComponentName}/{Constants.DeviceId}",
            new { },
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetValue_should_execute_a_button_feature()
    {
        Mock<IButtonFeature> feature = this.SetupNamedFeature<IButtonFeature>(Constants.ComponentName);
        feature.Setup(f => f.ExecuteAsync(Constants.DeviceId, It.IsAny<CancellationToken>())).ReturnsAsync(new SuccessResponse(true));
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.AdapterName}/{Constants.ComponentName}/{Constants.DeviceId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await DeviceEndpointsTests.ReadJsonAsync(response)).RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task GetValue_should_read_a_value_feature()
    {
        Mock<IValueFeature> feature = this.SetupNamedFeature<IValueFeature>(Constants.ComponentName);
        feature.Setup(f => f.GetValueAsync(Constants.DeviceId, It.IsAny<CancellationToken>())).ReturnsAsync(new ValueResponse(42));
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.AdapterName}/{Constants.ComponentName}/{Constants.DeviceId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(42, (await DeviceEndpointsTests.ReadJsonAsync(response)).RootElement.GetProperty("value").GetInt32());
    }

    [Fact]
    public async Task GetValue_should_fall_back_to_a_discovered_dynamic_device()
    {
        Mock<IDeviceAdapter> dynamicAdapter = new();
        Mock<IValueFeature> feature = new();
        feature.Setup(f => f.GetValueAsync(Constants.DeviceId, It.IsAny<CancellationToken>())).ReturnsAsync(new ValueResponse("dynamic"));
        dynamicAdapter.Setup(adapter => adapter.AdapterName).Returns("dynamic");
        dynamicAdapter.Setup(adapter => adapter.GetFeature(Constants.ComponentName)).Returns(feature.Object);
        this._mockDynamicRegistry
            .Setup(registry => registry.GetDiscoveredDeviceAsync(this._mockAdapter.Object, Constants.DeviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dynamicAdapter.Object);
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.AdapterName}/{Constants.ComponentName}/{Constants.DeviceId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("dynamic", (await DeviceEndpointsTests.ReadJsonAsync(response)).RootElement.GetProperty("value").GetString());
    }

    [Fact]
    public async Task GetValue_should_respond_not_found_for_an_unknown_component()
    {
        this._mockDynamicRegistry
            .Setup(registry => registry.GetDiscoveredDeviceAsync(this._mockAdapter.Object, Constants.DeviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDeviceAdapter?)null);
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.AdapterName}/{Constants.ComponentName}/{Constants.DeviceId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SetValue_should_pass_the_value_to_the_value_feature()
    {
        Mock<IValueFeature> feature = this.SetupNamedFeature<IValueFeature>(Constants.ComponentName);
        feature.Setup(f => f.SetValueAsync(Constants.DeviceId, "true", It.IsAny<CancellationToken>())).ReturnsAsync(new SuccessResponse(true));
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.AdapterName}/{Constants.ComponentName}/{Constants.DeviceId}/true", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await DeviceEndpointsTests.ReadJsonAsync(response)).RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task PerformDirectoryAction_should_pass_the_action_to_the_directory_feature()
    {
        Mock<IDirectoryFeature> feature = this.SetupNamedFeature<IDirectoryFeature>(Constants.ComponentName);
        feature.Setup(f => f.PerformActionAsync(Constants.DeviceId, "play", It.IsAny<CancellationToken>())).ReturnsAsync(new SuccessResponse(true));
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.PostAsJsonAsync(
            $"/device/{Constants.AdapterName}/{Constants.ComponentName}/{Constants.DeviceId}/action",
            new { actionIdentifier = "play" },
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        feature.Verify(f => f.PerformActionAsync(Constants.DeviceId, "play", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Custom_route_should_invoke_the_route_handler_for_any_http_method(string method)
    {
        string? receivedPath = default;
        string? receivedMethod = default;
        this._mockAdapter.Setup(adapter => adapter.RouteHandler).Returns((request, path, _) =>
        {
            (receivedPath, receivedMethod) = (path, request.Method);
            return Task.FromResult(Results.Text("handled"));
        });
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.SendAsync(
            new(new(method), $"/device/{Constants.AdapterName}/custom/images/cover.jpg"),
            TestContext.Current.CancellationToken
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("handled", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("images/cover.jpg", receivedPath);
        Assert.Equal(method, receivedMethod);
    }

    [Theory]
    [InlineData("/custom")]
    [InlineData("/custom/")]
    public async Task Custom_route_should_pass_an_empty_path_when_there_is_no_suffix(string suffix)
    {
        string? receivedPath = default;
        this._mockAdapter.Setup(adapter => adapter.RouteHandler).Returns((_, path, _) =>
        {
            receivedPath = path;
            return Task.FromResult(Results.Ok());
        });
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.AdapterName}{suffix}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(string.Empty, receivedPath);
    }

    [Fact]
    public async Task Custom_route_should_respond_with_the_result_returned_by_the_handler()
    {
        this._mockAdapter.Setup(adapter => adapter.RouteHandler).Returns((_, _, _) => Task.FromResult(Results.StatusCode(503)));
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.AdapterName}/custom/x", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Custom_route_should_respond_not_found_when_the_adapter_has_no_route_handler()
    {
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.AdapterName}/custom/x", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Custom_route_should_respond_not_found_for_an_unknown_adapter()
    {
        await using EndpointTestHost host = await this.StartAsync();

        using HttpResponseMessage response = await host.Client.GetAsync($"/device/{Constants.UnknownAdapterName}/custom/x", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

    private Mock<TFeature> SetupFeature<TFeature>(ComponentType type) where TFeature : class, IFeature
    {
        Mock<TFeature> feature = new();
        this._mockAdapter.Setup(adapter => adapter.GetFeature(type)).Returns(feature.Object);
        return feature;
    }

    private Mock<TFeature> SetupNamedFeature<TFeature>(string componentName) where TFeature : class, IFeature
    {
        Mock<TFeature> feature = new();
        this._mockAdapter.Setup(adapter => adapter.GetFeature(componentName)).Returns(feature.Object);
        return feature;
    }

    private Task<EndpointTestHost> StartAsync() => EndpointTestHost.StartAsync(
        services => services
            .AddSingleton(this._mockDatabase.Object)
            .AddSingleton(this._mockDynamicRegistry.Object)
            .AddSingleton(this._mockPgp.Object),
        endpoints => endpoints.MapDeviceEndpoints(),
        TestContext.Current.CancellationToken
    );

    private static class Constants
    {
        public const string AdapterName = "adapter";
        public const string ComponentName = "component";
        public const string DeviceId = "device-1";
        public const string UnknownAdapterName = "unknown";
    }
}
