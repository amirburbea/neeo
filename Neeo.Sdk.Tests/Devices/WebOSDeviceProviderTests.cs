using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Drivers.WebOS;
using Neeo.Drivers.WebOS.Models;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Sdk.Tests.Devices;

public sealed class WebOSDeviceProviderTests
{
    [Fact]
    public void DeviceBuilder_configures_expected_capabilities()
    {
        Mock<ISettingsStore<WebOSClientManager>> settingsMock = new();
        WebOSClientManager manager = new(
            NullLogger<WebOSClient>.Instance,
            NullLogger<WebOSClientManager>.Instance,
            settingsMock.Object
        );
        WebOSDeviceProvider provider = new(manager, NullLogger<WebOSDeviceProvider>.Instance);

        IDeviceBuilder builder = provider.DeviceBuilder;

        Assert.NotNull(builder);
        Assert.Equal("LG OLED TV", builder.Name);
        Assert.Equal("LG", builder.Manufacturer);
        Assert.True(builder.HasPowerStateSensor);
        Assert.Contains("Apps", builder.Directories);
    }

    [Theory]
    [InlineData(Buttons.CursorUp, "UP")]
    [InlineData(Buttons.CursorDown, "DOWN")]
    [InlineData(Buttons.CursorLeft, "LEFT")]
    [InlineData(Buttons.CursorRight, "RIGHT")]
    [InlineData(Buttons.CursorEnter, "ENTER")]
    [InlineData(Buttons.Back, "BACK")]
    [InlineData(Buttons.Home, "HOME")]
    [InlineData(Buttons.Menu, "MENU")]
    [InlineData(Buttons.FunctionRed, "RED")]
    [InlineData(Buttons.FunctionGreen, "GREEN")]
    [InlineData(Buttons.FunctionYellow, "YELLOW")]
    [InlineData(Buttons.FunctionBlue, "BLUE")]
    [InlineData(Buttons.VolumeUp, "VOLUMEUP")]
    [InlineData(Buttons.VolumeDown, "VOLUMEDOWN")]
    [InlineData(Buttons.MuteToggle, "MUTE")]
    public void WebOSRemoteKey_maps_buttons_correctly(Buttons button, string expectedKey)
    {
        Assert.True(WebOSRemoteKey.TryGetButtonName(button, out string actualKey));
        Assert.Equal(expectedKey, actualKey);
    }

    [Fact]
    public void WebOSResponse_deserializes_correctly()
    {
        const string json = """
        {
            "id": "sub_volume",
            "type": "response",
            "payload": {
                "returnValue": true,
                "volume": 35,
                "muted": false
            }
        }
        """;

        WebOSResponse response = JsonSerializer.Deserialize<WebOSResponse>(json, WebOSJsonSerializerOptions.Default);

        Assert.Equal("sub_volume", response.Id);
        Assert.Equal("response", response.Type);
        Assert.True(response.Payload.GetProperty("returnValue").GetBoolean());
        Assert.Equal(35, response.Payload.GetProperty("volume").GetInt32());
        Assert.False(response.Payload.GetProperty("muted").GetBoolean());
    }

    [Fact]
    public void WebOSAppInfo_deserializes_correctly()
    {
        const string json = """
        [
            { "id": "netflix", "title": "Netflix", "icon": "http://tv/netflix.png" },
            { "id": "youtube", "title": "YouTube", "icon": "http://tv/youtube.png" }
        ]
        """;

        WebOSAppInfo[]? apps = JsonSerializer.Deserialize<WebOSAppInfo[]>(json, WebOSJsonSerializerOptions.Default);

        Assert.NotNull(apps);
        Assert.Equal(2, apps.Length);
        Assert.Equal("netflix", apps[0].Id);
        Assert.Equal("Netflix", apps[0].Title);
        Assert.Equal("http://tv/netflix.png", apps[0].Icon);
    }

    [Fact]
    public void WebOSClient_implements_IDeviceClient_contract()
    {
        IPAddress ip = IPAddress.Parse("192.168.1.50");
        PhysicalAddress mac = PhysicalAddress.Parse("001122334455");
        using WebOSClient client = new(ip, mac, NullLogger.Instance);

        Assert.IsAssignableFrom<IDeviceClient>(client);
        Assert.Equal(mac.ToString(), client.DeviceId);
        Assert.False(client.IsConnected);
    }
}
