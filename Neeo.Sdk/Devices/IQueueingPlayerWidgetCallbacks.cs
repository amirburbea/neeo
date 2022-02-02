using System.Threading.Tasks;
using Neeo.Sdk.Devices.Lists;

namespace Neeo.Sdk.Devices;

public interface IQueueingPlayerWidgetCallbacks : IPlayerWidgetCallbacks
{
    Task HandleQueueDirectoryActionAsync(string deviceId, string actionIdentifier);

    Task PopulateQueueDirectoryAsync(string deviceId, IListBuilder builder);
}
