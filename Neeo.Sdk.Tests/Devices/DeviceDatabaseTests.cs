using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Notifications;
using Xunit;

namespace Neeo.Sdk.Tests.Devices;

public sealed class DeviceDatabaseTests
{
    [Fact]
    public void Adapters_should_expose_built_adapters()
    {
        var (mockBuilder, mockAdapter, _) = CreateDevice();

        var database = new DeviceDatabase(
           [mockBuilder.Object],
           Mock.Of<INotificationService>(),
           NullLogger<DeviceDatabase>.Instance
        );

        Assert.Equal(mockAdapter.Object, database.Adapters.Single());
    }

    [Fact]
    public void Constructor_should_build_all_adapters()
    {
        var mockBuilders = new Mock<IDeviceBuilder>[3];
        for (int i = 0; i < mockBuilders.Length; i++)
        {
            (mockBuilders[i], _, _) = CreateDevice($"device{i}");
        }

        _ = new DeviceDatabase(
            Array.ConvertAll(mockBuilders, static mockDevice => mockDevice.Object),
            Mock.Of<INotificationService>(),
            NullLogger<DeviceDatabase>.Instance
        );

        foreach (var mockBuilder in mockBuilders)
        {
            mockBuilder.Verify(builder => builder.BuildAdapter(), Times.Once);
        }
    }

    [Fact]
    public void Constructor_should_call_back_with_device_notifier()
    {
        var (mockBuilder, _, mockCallback) = CreateDevice(notifierCallback: true);

        _ = new DeviceDatabase(
            [mockBuilder.Object],
            Mock.Of<INotificationService>(),
            NullLogger<DeviceDatabase>.Instance
        );

        mockCallback!.Verify(callback => callback(It.IsAny<IDeviceNotifier>()), Times.Once());
    }

    [Fact]
    public void Constructor_should_throw_if_names_not_unique()
    {
        var (mockBuilder1, _, _) = CreateDevice("not_unique_name");
        var (mockBuilder2, _, _) = CreateDevice("not_unique_name");

        Assert.Throws<ArgumentException>(() => new DeviceDatabase(
            [mockBuilder1.Object, mockBuilder2.Object],
            Mock.Of<INotificationService>(),
            NullLogger<DeviceDatabase>.Instance
        ));
    }

    [Fact]
    public void GetAdapterAsync_calls_initializer_before_returning_adapter()
    {
        var (mockBuilder, mockAdapter, _) = CreateDevice("name");
        var mockInitializer = new Mock<DeviceInitializer>(MockBehavior.Strict);
        mockInitializer.Setup(initializer => initializer(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockAdapter.Setup(adapter => adapter.Initializer).Returns(mockInitializer.Object);
        DeviceDatabase database = new(
            [mockBuilder.Object],
            Mock.Of<INotificationService>(),
            NullLogger<DeviceDatabase>.Instance
        );

        _ = database.GetAdapterAsync("name").AsTask();

        mockInitializer.Verify(initializer => initializer(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public void GetAdapterAsync_does_not_call_initializer_if_already_initializing()
    {
        var (mockBuilder, mockAdapter, _) = CreateDevice("name");
        Mock<DeviceInitializer> mockInitializer = new(MockBehavior.Strict);
        TaskCompletionSource source = new();
        mockInitializer.Setup(initializer => initializer(It.IsAny<CancellationToken>())).Returns(source.Task);
        mockAdapter.Setup(adapter => adapter.Initializer).Returns(mockInitializer.Object);
        DeviceDatabase database = new(
            [mockBuilder.Object],
            Mock.Of<INotificationService>(),
            NullLogger<DeviceDatabase>.Instance
        );

        for (int i = 0; i < 5; i++)
        {
            _ = database.GetAdapterAsync("name").AsTask();
        }
        source.SetResult();

        mockInitializer.Verify(initializer => initializer(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public void GetAdapterAsync_does_not_call_initializer_if_initialized()
    {
        var (mockBuilder, mockAdapter, _) = CreateDevice("name");
        var mockInitializer = new Mock<DeviceInitializer>(MockBehavior.Strict);
        mockInitializer.Setup(initializer => initializer(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        mockAdapter.Setup(adapter => adapter.Initializer).Returns(mockInitializer.Object);
        DeviceDatabase database = new(
            [mockBuilder.Object],
            Mock.Of<INotificationService>(),
            NullLogger<DeviceDatabase>.Instance
        );

        for (int i = 0; i < 2; i++)
        {
            _ = database.GetAdapterAsync("name").AsTask();
        }

        mockInitializer.Verify(initializer => initializer(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GetAdapterAsync_returns_built_adapter()
    {
        var (mockBuilder, mockAdapter, _) = CreateDevice("name");
        mockAdapter.Setup(adapter => adapter.Initializer).Returns(value: null);
        DeviceDatabase database = new(
            [mockBuilder.Object],
            Mock.Of<INotificationService>(),
            NullLogger<DeviceDatabase>.Instance
        );

        Assert.Equal(mockAdapter.Object, await database.GetAdapterAsync("name"));
    }

    [Fact]
    public async Task GetAdapterAsync_returns_null_for_invalid_name()
    {
        DeviceDatabase database = new(
            [],
            Mock.Of<INotificationService>(),
            NullLogger<DeviceDatabase>.Instance
        );

        Assert.Null(await database.GetAdapterAsync("name"));
    }

    [Fact]
    public void GetDeviceByAdapterName_should_expose_model_of_adapters()
    {
        var builders = new IDeviceBuilder[3];
        for (int i = 0; i < builders.Length; i++)
        {
            var (mockBuilder, _, _) = CreateDevice($"device{i}", [$"token{i}"]);
            builders[i] = mockBuilder.Object;
        }

        DeviceDatabase database = new(
           builders,
           Mock.Of<INotificationService>(),
           NullLogger<DeviceDatabase>.Instance
        );

        for (int i = 0; i < builders.Length; i++)
        {
            var builder = builders[i];
            Assert.Equal(builder.AdapterName, database.GetDeviceByAdapterName(builder.AdapterName)!.AdapterName);
            Assert.Equal($"token{i}", database.GetDeviceByAdapterName(builder.AdapterName)!.Tokens);
        }
    }

    [Fact]
    public void GetDeviceByAdapterName_should_return_null_for_invalid_name()
    {
        DeviceDatabase database = new(
            [],
            Mock.Of<INotificationService>(),
            NullLogger<DeviceDatabase>.Instance
        );

        Assert.Null(database.GetDeviceByAdapterName("name"));
    }

    [Fact]
    public void GetDeviceById_should_expose_model_of_adapters()
    {
        var builders = new IDeviceBuilder[3];
        for (int i = 0; i < builders.Length; i++)
        {
            var (mockBuilder, _, _) = CreateDevice($"device{i}");
            builders[i] = mockBuilder.Object;
        }

        DeviceDatabase database = new(
           builders,
           Mock.Of<INotificationService>(),
           NullLogger<DeviceDatabase>.Instance
        );

        for (int i = 0; i < builders.Length; i++)
        {
            var builder = builders[i];
            Assert.Equal(builder.AdapterName, database.GetDeviceById(i)!.AdapterName);
        }
    }

    [Fact]
    public void GetDeviceById_should_return_null_for_invalid_id()
    {
        DeviceDatabase database = new(
           [],
           Mock.Of<INotificationService>(),
           NullLogger<DeviceDatabase>.Instance
        );

        Assert.Null(database.GetDeviceById(5));
    }

    private static (Mock<IDeviceBuilder>, Mock<IDeviceAdapter>, Mock<DeviceNotifierCallback>?) CreateDevice(string? name = null, string[]? tokens = null, bool notifierCallback = false)
    {
        Mock<IDeviceBuilder> mockBuilder = new(MockBehavior.Strict);
        Mock<IDeviceAdapter> mockAdapter = new(MockBehavior.Strict);
        mockBuilder.Setup(builder => builder.BuildAdapter()).Returns(mockAdapter.Object);
        string adapterName = name ?? nameof(mockAdapter);
        mockBuilder.Setup(builder => builder.AdapterName).Returns(adapterName);
        mockAdapter.Setup(adapter => adapter.DeviceName).Returns(value: adapterName);
        mockAdapter.Setup(adapter => adapter.AdapterName).Returns(adapterName);
        mockAdapter.Setup(adapter => adapter.Initializer).Returns(value: null);
        mockAdapter.Setup(adapter => adapter.Tokens).Returns(tokens ?? []);
        mockBuilder.Setup(builder => builder.HasPowerStateSensor).Returns(true);
        var mockCallback = notifierCallback
            ? new Mock<DeviceNotifierCallback>(MockBehavior.Strict)
            : null;
        mockCallback?.Setup(callback => callback(It.IsAny<IDeviceNotifier>()));
        mockBuilder.Setup(builder => builder.NotifierCallback).Returns(mockCallback?.Object);
        return (mockBuilder, mockAdapter, mockCallback);
    }
}
