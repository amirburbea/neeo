using System.Threading.Tasks;

namespace Remote.Neeo.Devices.Discovery
{
    public delegate Task SecurityCodeRegistration(string securityCode);

    public sealed class SecurityCodeRegistrationOptions
    {
    }
}
