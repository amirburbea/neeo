using Microsoft.AspNetCore.Mvc;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities.TokenSearch;

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
    public ActionResult<SearchEntry<DeviceModel>[]> Search([FromQuery(Name = "q")] string? query)
    {
        return database.Search(query);
    }
}
