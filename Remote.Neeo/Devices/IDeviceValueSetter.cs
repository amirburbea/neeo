using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    public interface IDeviceValueSetter<TValue>
    {
        Task SetValueAsync(string deviceId, TValue value);
    }
}
