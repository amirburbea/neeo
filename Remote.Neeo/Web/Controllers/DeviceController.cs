using Microsoft.AspNetCore.Mvc;
using Remote.Neeo.Devices;

namespace Remote.Neeo.Web.Controllers
{
    [ApiController, Route("[controller]")]
    internal sealed class DeviceController : ControllerBase
    {
        private readonly Brain _brain;
        private readonly IDeviceDatabase _database;

        public DeviceController(Brain brain, IDeviceDatabase database)
        {
            this._brain = brain;
            this._database = database;
        }
    }
}