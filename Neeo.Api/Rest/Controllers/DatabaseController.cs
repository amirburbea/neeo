﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Neeo.Api.Devices;
using Neeo.Api.Utilities.TokenSearch;

namespace Neeo.Api.Rest.Controllers;

[ApiController, Route("db")]
internal sealed class DatabaseController : ControllerBase
{
    private readonly IDeviceDatabase _database;

    public DatabaseController(IDeviceDatabase database) => this._database = database;

    [HttpGet("adapterdefinition/{adapterName}")]
    public ActionResult<IDeviceModel> GetDeviceByAdapterName(string adapterName) => new(this._database.GetDeviceByAdapterName(adapterName));

    [HttpGet("{deviceId}")]
    public ActionResult<IDeviceModel> GetDevice(int deviceId) => new(this._database.GetDevice(deviceId));

    [HttpGet("search")]
    public ActionResult<IEnumerable<ISearchItem<IDeviceModel>>> Search([FromQuery(Name = "q")] string? query = null)
    {
        var list = this._database.Search(query).ToList();
        return list;
    }
}