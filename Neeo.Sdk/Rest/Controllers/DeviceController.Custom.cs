﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [Route("{adapterName}/custom/{**suffix}")]
    public async Task<ActionResult> ProcessCustomRouteAsync(string adapterName, string suffix) => await this._database.GetAdapterAsync(adapterName) is { RouteHandler: { } handler }
        ? await handler(this.HttpContext, suffix)
        : this.NotFound();
}