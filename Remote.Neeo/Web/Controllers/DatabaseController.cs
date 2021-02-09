using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Remote.Neeo.Devices;

namespace Remote.Neeo.Web.Controllers
{
    [ApiController, Route("db")]
    internal sealed class DatabaseController : ControllerBase
    {
        private readonly IDeviceDatabase _database;

        public DatabaseController(IDeviceDatabase database) => this._database = database;

        [HttpGet("adapterdefinition/{adapterName}")]
        public ActionResult<object> GetAdapterDefinition(string adapterName)
        {
            return this.Ok(adapterName);
        }

        [HttpGet("{deviceId}")]
        public ActionResult<object> GetDevice(string deviceId)
        {
            return this.Ok(deviceId);
        }

        [HttpGet("search")]
        public ActionResult<IReadOnlyCollection<SearchResult<IDeviceModel>>> Search([FromQuery(Name = "q")] string? query = null)
        {
            IEnumerable<SearchResult<IDeviceModel>> results = this._database.Search(query);
            return this.Ok(results as IReadOnlyCollection<SearchResult<IDeviceModel>> ?? results.ToList());
        }
    }
}
