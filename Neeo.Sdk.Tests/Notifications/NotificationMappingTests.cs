using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Notifications;
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
        Mock<ISdkEnvironment> mockEnvironment = new();
        mockEnvironment.SetupGet(environment => environment.SdkAdapterName).Returns(Constants.SdkAdapterName);
        List<string> paths = new();
        Mock<IApiClient> mockClient = new();
        mockClient.Setup(client => client.GetAsync(
            Capture.In(paths),
            It.IsAny<Func<Entry[], Entry[]>>(),
            It.IsAny<CancellationToken>()
        )).Returns(Task.FromResult(NotificationMappingTests._entries));
        this._path = new(() => paths.Single());
        this._notificationMapping = new(mockClient.Object, mockEnvironment.Object, NullLogger<NotificationMapping>.Instance);
    }

    [Fact]
    public async Task Should_Get_Keys_Of_Entries_Matching_Label_If_Not_Matched_By_Name()
    {
        var keys = await this.GetNotificationKeysAsync(string.Empty, string.Empty, "label2").ConfigureAwait(false);
        Assert.Equal(new[] { "key2", "key3" }, keys);
    }

    [Fact]
    public async Task Should_Get_Keys_Of_Entries_Matching_Name()
    {
        var keys = await this.GetNotificationKeysAsync(string.Empty, string.Empty, "name1").ConfigureAwait(false);
        Assert.Equal("key1", keys.Single());
    }

    [Fact]
    public async Task Should_Make_Http_Request_To_Correct_Path()
    {
        await this.GetNotificationKeysAsync("myAdapter", "myDevice", "myComponent").ConfigureAwait(false);
        Assert.Equal($"/v1/api/notificationkey/{Constants.SdkAdapterName}/myAdapter/myDevice", this._path.Value);
    }

    private ValueTask<string[]> GetNotificationKeysAsync(string adapterName, string deviceId, string componentName)
    {
        Mock<IDeviceAdapter> mockAdapter = new();
        mockAdapter.SetupGet(adapter => adapter.AdapterName).Returns(adapterName);
        mockAdapter.SetupGet(adapter => adapter.DeviceName).Returns(adapterName);
        return this._notificationMapping.GetNotificationKeysAsync(mockAdapter.Object, deviceId, componentName, default);
    }

    private static class Constants
    {
        public const string SdkAdapterName = "sdk";
    }
}