using Microsoft.AspNetCore.Mvc;
using Remote.Neeo.Devices;

namespace Remote.Neeo.Web.Controllers
{
    [ApiController, Route("[controller]")]
    internal sealed class DeviceController : ControllerBase
    {
        private readonly IDeviceDatabase _database;

        public DeviceController(IDeviceDatabase database)
        {
            this._database = database;
        }



    }
}
