using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    public interface IButtonHandler
    {
        Task HandleButtonPressAsync(string button, string deviceId);
    }
}
