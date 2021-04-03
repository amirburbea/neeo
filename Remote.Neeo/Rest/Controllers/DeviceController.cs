using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Remote.Neeo.Devices;
using Remote.Neeo.Devices.Components;

namespace Remote.Neeo.Rest.Controllers
{
    [ApiController, Route("[controller]")]
    internal sealed class DeviceController : ControllerBase
    {
        private readonly IDeviceDatabase _database;

        public DeviceController(IDeviceDatabase database) => this._database = database;

        //private Task<IDeviceAdapter> GetAdapterAsync(string adapterName) => this._database.GetAdapterAsync(adapterName);

        public async Task<ActionResult<bool>> GetIsRegistered(string adapterName)
        {
            IDeviceAdapter adapter = await this._database.GetAdapterAsync(adapterName);
            var handler = adapter.GetHandler(ComponentType.Discovery);
            if (handler != null)
            {
                var value = await handler.Controller.GetValueAsync(adapter.AdapterName);

            }

            throw new();
        }
    }
}