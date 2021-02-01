using System;
using Microsoft.AspNetCore.Mvc;

namespace Remote.Neeo.Server.Controllers
{
    [ApiController, Route("db")]
    public sealed class DatabaseController : ControllerBase
    {
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
