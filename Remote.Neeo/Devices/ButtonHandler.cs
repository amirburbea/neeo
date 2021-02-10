using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    public delegate Task ButtonHandler(string deviceId, string button);
}
