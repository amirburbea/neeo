using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    public interface IFavoritesHandler
    {
        Task ExecuteAsync(string deviceId, string channel);
    }
}