using System;

namespace Neeo.Sdk.Devices;

public interface IDeviceFactory
{
    IDeviceAdapter BuildDevice(IDeviceBuilder builder);
}

internal sealed class DeviceFactory : IDeviceFactory
{
    IDeviceAdapter IDeviceFactory.BuildDevice(IDeviceBuilder builder) => DeviceFactory.BuildAdapter(builder);

    private static DeviceAdapter BuildAdapter(IDeviceBuilder builder) => (builder as DeviceBuilder ?? throw new NotSupportedException()).BuildAdapter();
}