using System;
using Microsoft.AspNetCore.Mvc;

namespace Remote.Neeo.Web.Controllers
{
    [ApiController, Route("db")]
    internal sealed class DatabaseController : ControllerBase
    {
        private readonly DeviceDatabase _deviceDatabase;

        public DatabaseController(DeviceDatabase deviceDatabase)
        {
            this._deviceDatabase = deviceDatabase;
        }

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
        public ActionResult<Array> Search([FromQuery(Name = "q")] string? query = null)
        {
            if (String.IsNullOrEmpty(query))
            {
                return this.Ok(Array.Empty<object>());
            }
            return this.Ok( new[] { query });
        }
    }
}
