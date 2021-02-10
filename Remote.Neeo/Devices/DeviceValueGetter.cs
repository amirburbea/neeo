using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    public delegate Task<object> DeviceValueGetter(string deviceId);

    public delegate Task<TValue> DeviceValueGetter<TValue>(string deviceId);
}
