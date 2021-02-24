using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    public delegate Task DeviceListInitializer(string[] deviceIds);
}