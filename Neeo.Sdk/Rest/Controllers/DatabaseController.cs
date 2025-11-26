using Microsoft.AspNetCore.Mvc;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Rest.Controllers;

[ApiController, Route("db")]
internal sealed class DatabaseController(IDeviceDatabase database) : ControllerBase
{
    [HttpGet("adapterdefinition/{adapterName}")]
    public ActionResult<DeviceModel> GetDeviceByAdapterName(string adapterName)
    {
        return database.GetDeviceByAdapterName(adapterName) is not { } device
            ? this.NotFound()
            : device;
    }

    [HttpGet("{deviceId}")]
    public ActionResult<DeviceModel> GetDeviceById(int deviceId)
    {
        return database.GetDeviceById(deviceId) is not { } device
            ? this.NotFound()
            : device;
    }

    [HttpGet("search")]
    public ActionResult<DeviceSearchResult[]> Search([FromQuery(Name = "q")] string? query)
    {
        return database.Search(query);
    }
}
