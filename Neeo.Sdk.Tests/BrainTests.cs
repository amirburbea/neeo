using System;
using System.Net;
using System.Threading.Tasks;
using Moq;
using Neeo.Sdk.Devices;
using Xunit;

namespace Neeo.Sdk.Tests;

public sealed class BrainTests
{
    [Fact]
    public void Constructor_should_initialize_valid_brain()
    {
        IPAddress address = IPAddress.Parse("192.168.1.100");
        Brain brain = new(address, "MyBrain", "0.53.1");

        Assert.Equal("MyBrain", brain.HostName);
        Assert.Equal(address, brain.IPAddress);
        Assert.Equal(new IPEndPoint(address, Brain.ServicePort), brain.ServiceEndPoint);
        Assert.Equal("0.53.1", brain.Version);
    }

    [Fact]
    public void Constructor_should_default_hostname_and_version()
    {
        IPAddress address = IPAddress.Parse("192.168.1.50");
        Brain brain = new(address);

        Assert.Equal("192.168.1.50", brain.HostName);
        Assert.Equal("0.50.0", brain.Version);
        Assert.Equal(Brain.ServicePort, brain.ServiceEndPoint.Port);
    }

    [Fact]
    public void Constructor_should_throw_on_non_ipv4_address()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Brain(IPAddress.IPv6Loopback));
        Assert.Equal("ipAddress", exception.ParamName);
    }

    [Theory]
    [InlineData("0.49.0")]
    [InlineData("0.4.1")]
    [InlineData("invalid")]
    public void Constructor_should_throw_on_incompatible_version(string version)
    {
        Assert.Throws<InvalidOperationException>(() => new Brain(IPAddress.Loopback, version: version));
    }

    [Fact]
    public async Task StartServerAsync_device_builders_should_throw_on_null_brain()
    {
        Brain brain = null!;
        IDeviceBuilder[] builders = [Mock.Of<IDeviceBuilder>()];

        await Assert.ThrowsAsync<ArgumentNullException>(() => brain.StartServerAsync(builders, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StartServerAsync_device_builders_should_throw_on_empty_devices()
    {
        Brain brain = new(IPAddress.Loopback);

        await Assert.ThrowsAsync<ArgumentException>(() => brain.StartServerAsync(Array.Empty<IDeviceBuilder>(), cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StartServerAsync_provider_types_should_throw_on_null_brain()
    {
        Brain brain = null!;
        Type[] providerTypes = [typeof(ValidDeviceProvider)];

        await Assert.ThrowsAsync<ArgumentNullException>(() => brain.StartServerAsync(providerTypes, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StartServerAsync_provider_types_should_throw_on_null_types()
    {
        Brain brain = new(IPAddress.Loopback);

        await Assert.ThrowsAsync<ArgumentNullException>(() => brain.StartServerAsync((Type[])null!, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StartServerAsync_provider_types_should_throw_on_empty_types()
    {
        Brain brain = new(IPAddress.Loopback);

        await Assert.ThrowsAsync<ArgumentException>(() => brain.StartServerAsync(Array.Empty<Type>(), cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task StartServerAsync_provider_types_should_throw_on_non_provider_type()
    {
        Brain brain = new(IPAddress.Loopback);

        await Assert.ThrowsAsync<ArgumentException>(() => brain.StartServerAsync([typeof(string)], cancellationToken: TestContext.Current.CancellationToken));
    }

    private sealed class ValidDeviceProvider : IDeviceProvider
    {
        public IDeviceBuilder DeviceBuilder => Mock.Of<IDeviceBuilder>();
    }
}
