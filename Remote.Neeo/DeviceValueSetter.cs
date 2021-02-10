using System.Threading.Tasks;

namespace Remote.Neeo
{
    public delegate Task DeviceValueSetter(string deviceId, object value);

    public delegate Task DeviceValueSetter<TValue>(string deviceId, TValue value);
}
