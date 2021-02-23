using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Remote.Neeo.Devices;

namespace Remote.Neeo.Web.Controllers
{
    [ApiController, Route("[controller]")]
    internal sealed class DeviceController : ControllerBase
    {
        private readonly IDeviceDatabase _database;

        public DeviceController(IDeviceDatabase database) => this._database = database;

        //private Task<IDeviceAdapter> GetAdapterAsync(string adapterName) => this._database.GetAdapterAsync(adapterName);

        public async Task<ActionResult<bool>> GetIsRegistered(string adapterName)
        {
            IDeviceAdapter adapter = await this._database.GetAdapterAsync(adapterName).ConfigureAwait(false);

            adapter.GetHandler(ComponentType.Registration);

            throw new();
        }
    }
}