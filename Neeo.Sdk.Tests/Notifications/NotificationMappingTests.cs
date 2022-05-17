using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Notifications;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Sdk.Tests.Notifications;

using Entry = NotificationMapping.Entry;

public sealed class NotificationMappingTests
{
    private static readonly Entry[] _entries =
    {
        new("key0", Name: "name0", Label: "name1"),
        new("key1", Name: "name1", Label: "name1"),
        new("key2", Name: "name2", Label: "label2"),
        new("key3", Name: "name3", Label: "label2"),
    };

    private readonly NotificationMapping _notificationMapping;
    private readonly Lazy<string> _path;

    public NotificationMappingTests()
    {
        Mock<ISdkEnvironment> mockEnvironment = new(MockBehavior.Strict);
        mockEnvironment.Setup(environment => environment.SdkAdapterName).Returns(Constants.SdkAdapterName);
        List<string> paths = new();
        Mock<IApiClient> mockClient = new(MockBehavior.Strict);
        mockClient
            .Setup(client => client.GetAsync(Capture.In(paths), It.IsAny<Func<Entry[], Entry[]>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(NotificationMappingTests._entries);
        this._path = new(() => paths.Single());
        this._notificationMapping = new(mockClient.Object, mockEnvironment.Object, NullLogger<NotificationMapping>.Instance);
    }

    [Fact]
    public async Task Should_fall_back_to_get_keys_from_entries_by_label_when_not_matching_by_name()
    {
        var keys = await this.GetNotificationKeysAsync(string.Empty, string.Empty, "label2");

        Assert.Equal(new[] { "key2", "key3" }, keys);
    }

    [Fact]
    public async Task Should_get_keys_from_entries_matching_by_name()
    {
        var keys = await this.GetNotificationKeysAsync(string.Empty, string.Empty, "name1");
 
        Assert.Equal("key1", keys.Single());
    }

    [Fact]
    public async Task Should_make_API_request_to_correct_path()
    {
        await this.GetNotificationKeysAsync("myAdapter", "myDevice", "myComponent");

        Assert.Equal($"/v1/api/notificationkey/{Constants.SdkAdapterName}/myAdapter/myDevice", this._path.Value);
    }

    private ValueTask<string[]> GetNotificationKeysAsync(string adapterName, string deviceId, string componentName)
    {
        Mock<IDeviceAdapter> mockAdapter = new(MockBehavior.Strict);
        mockAdapter.Setup(adapter => adapter.AdapterName).Returns(adapterName);
        mockAdapter.Setup(adapter => adapter.DeviceName).Returns(adapterName);
        return this._notificationMapping.GetNotificationKeysAsync(mockAdapter.Object, deviceId, componentName, default);
    }

    private static class Constants
    {
        public const string SdkAdapterName = "sdk";
    }
}