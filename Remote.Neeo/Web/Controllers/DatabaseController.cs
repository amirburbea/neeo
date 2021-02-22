using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Remote.Neeo.Devices;
using Remote.Utilities.TokenSearch;

namespace Remote.Neeo.Web.Controllers
{
    [ApiController, Route("db")]
    internal sealed class DatabaseController : ControllerBase
    {
        private readonly IDeviceDatabase _database;

        public DatabaseController(IDeviceDatabase database) => this._database = database;

        [HttpGet("adapterdefinition/{adapterName}")]
        public ActionResult<IDeviceModel> GetDeviceByAdapterName(string adapterName) => this.Ok(this._database.GetDeviceByAdapterName(adapterName));

        [HttpGet("{deviceId}")]
        public ActionResult<IDeviceModel> GetDevice(int deviceId) => this.Ok(this._database.GetDevice(deviceId));

        [HttpGet("search")]
        public ActionResult<IEnumerable<SearchItem<IDeviceModel>>> Search([FromQuery(Name = "q")] string? query = null)
        {
            return this.Ok(this._database.Search(query));
        }
    }
}