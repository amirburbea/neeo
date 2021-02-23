using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    public delegate Task DeviceSubscriptionHandler(string deviceId);
}