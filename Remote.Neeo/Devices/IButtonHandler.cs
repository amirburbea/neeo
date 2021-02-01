using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    public interface IButtonHandler
    {
        Task HandleButtonAsync(string button, string deviceId);
    }
}
