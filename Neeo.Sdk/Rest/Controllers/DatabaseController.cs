using Microsoft.AspNetCore.Mvc;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities.TokenSearch;

namespace Neeo.Sdk.Rest.Controllers;

[ApiController, Route("db")]
internal sealed class DatabaseController(IDeviceDatabase database) : ControllerBase
{
    [HttpGet("adapterdefinition/{adapterName}")]
    public ActionResult<DeviceAdapterModel> GetDeviceByAdapterName(string adapterName)
    {
        return database.GetDeviceByAdapterName(adapterName) is not { } device
            ? this.NotFound()
            : device;
    }

    [HttpGet("{deviceId}")]
    public ActionResult<DeviceAdapterModel> GetDeviceById(int deviceId)
    {
        return database.GetDeviceById(deviceId) is not { } device
            ? this.NotFound()
            : device;
    }

    [HttpGet("search")]
    public ActionResult<SearchEntry<DeviceAdapterModel>[]> Search([FromQuery(Name = "q")] string? query) => database.Search(query);
}
