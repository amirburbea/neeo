namespace Neeo.Api.Devices;

public interface IDynamicDeviceRegistrar
{
    void RegisterDiscoveredDevice(string deviceId, IDeviceAdapter adapter);
}