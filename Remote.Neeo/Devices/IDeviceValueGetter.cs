using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    public interface IDeviceValueGetter<TValue>
    {
        Task<TValue> GetValueAsync(string deviceId);
    }
}
