using System.Threading.Tasks;

namespace Remote.Neeo.Devices
{
    public interface ISensorController
    {
        internal Task<object> GetValueAsync(string deviceId);
    }

    public interface ISensorController<T> : ISensorController
        where T : notnull
    {
        new Task<T> GetValueAsync(string deviceId);

        Task<object> ISensorController.GetValueAsync(string deviceId) => this.GetValueAsync(deviceId).ContinueWith(task => (object)task.Result, TaskContinuationOptions.ExecuteSynchronously);
    }
}
