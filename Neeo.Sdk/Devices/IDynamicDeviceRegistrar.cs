namespace Neeo.Sdk.Devices;

public interface IDynamicDeviceRegistrar
{
    void RegisterDiscoveredDevice(string deviceId, IDeviceAdapter adapter);
}