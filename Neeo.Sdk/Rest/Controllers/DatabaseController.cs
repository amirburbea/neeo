using Microsoft.AspNetCore.Mvc;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities.TokenSearch;

namespace Neeo.Sdk.Rest.Controllers;

[ApiController, Route("db")]
internal sealed class DatabaseController : ControllerBase
{
    private readonly IDeviceDatabase _database;

    public DatabaseController(IDeviceDatabase database) => this._database = database;

    [HttpGet("adapterdefinition/{adapterName}")]
    public ActionResult<IDeviceModel> GetDeviceByAdapterName(string adapterName) => this._database.GetDeviceByAdapterName(adapterName) is not { } device
        ? this.NotFound()
        : this.Ok<IDeviceModel>(device);

    [HttpGet("{deviceId}")]
    public ActionResult<IDeviceModel> GetDeviceById(int deviceId) => this._database.GetDeviceById(deviceId) is not { } device
        ? this.NotFound()
        : this.Ok<IDeviceModel>(device);

    [HttpGet("search")]
    public ActionResult<SearchEntry<IDeviceModel>[]> Search([FromQuery(Name = "q")] string? query) => this.Ok(this._database.Search(query));
}