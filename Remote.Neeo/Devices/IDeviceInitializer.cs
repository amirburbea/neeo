using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    public interface IDeviceInitializer
    {
        Task InitializeAsync();
    }
}
