using System.Text.Json;
using System.Text.Json.Serialization;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Devices.Components;
using Neeo.Sdk.Devices.Directories;
using Neeo.Sdk.Notifications;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Sdk.Tests.Utilities;

/// <summary>
/// Guards wire compatibility with the NEEO Brain: the source-generated <see cref="AppJsonSerializerContext"/>
/// (via <see cref="AppJsonSerializerOptions.Default"/>) must produce byte-identical JSON to whatever each
/// type's actual current production path is - for <see cref="DeviceModel"/>/<see cref="DirectoryData"/>
/// that's the MVC pipeline's camelCase+ignore-null options (see <c>Server.ConfigureJsonOptions</c>), for
/// <see cref="NotificationService.Message"/> it's the raw <see cref="JsonSerializerOptions.Web"/> default
/// used directly in <c>NotificationService</c> today. The Brain firmware cannot be modified, so any drift
/// here is a bug.
/// </summary>
public sealed class JsonSerializationCompatibilityTests
{
    // Mirrors Server.ConfigureJsonOptions - the actual options MVC controllers serialize DeviceModel/
    // DirectoryData with today, which differs from the raw JsonSerializerOptions.Web default (it ignores
    // nulls; .Web does not).
    private static readonly JsonSerializerOptions _currentMvcOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };


    [Fact]
    public void DeviceModel_serializes_identically_via_reflection_and_source_gen()
    {
        Mock<IDeviceAdapter> mockAdapter = new(MockBehavior.Strict);
        Component[] components =
        [
            new SwitchComponent("power", "Power", "power", "power_SENSOR"),
            new SliderComponent("volume", "Volume", "volume", new([0, 100], "%", "volume_SENSOR")),
            new TextLabelComponent("status", "Status", "status", IsLabelVisible: true, "status_SENSOR"),
            new SensorComponent("battery", "Battery", "battery", new RangeSensorDetails([0, 100], "%")),
            new ImageUrlComponent("cover", "Cover", "cover", ImageSize.Large, "cover_SENSOR"),
            new DirectoryComponent("root", "Root", "root", DirectoryRole.Root),
        ];
        mockAdapter.Setup(adapter => adapter.AdapterName).Returns("adapter-1");
        mockAdapter.Setup(adapter => adapter.Components).Returns(components);
        mockAdapter.Setup(adapter => adapter.DeviceCapabilities).Returns([DeviceCapability.AlwaysOn]);
        mockAdapter.Setup(adapter => adapter.DeviceName).Returns("Test Device");
        mockAdapter.Setup(adapter => adapter.DriverVersion).Returns(2);
        mockAdapter.Setup(adapter => adapter.Icon).Returns(DeviceIconOverride.Sonos);
        mockAdapter.Setup(adapter => adapter.Manufacturer).Returns("Acme");
        mockAdapter.Setup(adapter => adapter.Setup).Returns(new DeviceSetup());
        mockAdapter.Setup(adapter => adapter.SpecificName).Returns("Specific Name");
        mockAdapter.Setup(adapter => adapter.Timing).Returns(new DeviceTiming { PowerOnDelay = 500 });
        mockAdapter.Setup(adapter => adapter.Tokens).Returns(["alpha", "beta"]);
        mockAdapter.Setup(adapter => adapter.Type).Returns(DeviceType.Accessory);
        DeviceModel model = new(1, mockAdapter.Object);

        string reflectionJson = JsonSerializer.Serialize(model, JsonSerializationCompatibilityTests._currentMvcOptions);
        string sourceGenJson = JsonSerializer.Serialize(model, AppJsonSerializerOptions.Default);

        Assert.Equal(reflectionJson, sourceGenJson);
    }

    [Fact]
    public void DirectoryData_serializes_identically_via_reflection_and_source_gen()
    {
        DirectoryBuilder builder = new(new(BrowseIdentifier: "root", Limit: 64));
        builder
            .AddHeader("Header")
            .AddEntry(new DirectoryEntry("Movies", ThumbnailUri: "http://example.com/movies.png", BrowseIdentifier: ".movies"))
            .AddTileRow(new DirectoryTile("http://example.com/tile.png", ActionIdentifier: "TILE1"))
            .AddButtonRow(new DirectoryButton("Shuffle", Icon: DirectoryButtonIcon.Shuffle))
            .AddInfoItem(new DirectoryInfoItem("Click me", "Are you sure?", AffirmativeButtonText: "Yes", NegativeButtonText: "No"));
        DirectoryData data = builder.Build();

        string reflectionJson = JsonSerializer.Serialize(data, JsonSerializationCompatibilityTests._currentMvcOptions);
        string sourceGenJson = JsonSerializer.Serialize(data, AppJsonSerializerOptions.Default);

        Assert.Equal(reflectionJson, sourceGenJson);
    }

    [Fact]
    public void NotificationServiceMessage_serializes_identically_via_reflection_and_source_gen()
    {
        NotificationService.Message valueMessage = new("EVENT_KEY", "some-value", isSensorNotification: false);
        NotificationService.Message sensorMessage = new("EVENT_KEY", true, isSensorNotification: true);

        Assert.Equal(
            JsonSerializer.Serialize(valueMessage, JsonSerializerOptions.Web),
            JsonSerializer.Serialize(valueMessage, AppJsonSerializerOptions.Default)
        );
        Assert.Equal(
            JsonSerializer.Serialize(sensorMessage, JsonSerializerOptions.Web),
            JsonSerializer.Serialize(sensorMessage, AppJsonSerializerOptions.Default)
        );
    }
}
