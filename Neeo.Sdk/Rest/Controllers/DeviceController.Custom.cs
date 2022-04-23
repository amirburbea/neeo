﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Neeo.Sdk.Rest.Controllers;

internal partial class DeviceController
{
    [Route("{adapterName}/custom/{**suffix}")]
    public async Task<ActionResult> HandleCustomRouteAsync(string adapterName, string suffix)
    {
        if (await this._database.GetAdapterAsync(adapterName) is not { } adapter || adapter.RouteHandler is not { } handler)
        {
            return this.NotFound();
        }
        this._logger.LogInformation("Running route handler for {adapter} with \"{suffix}\"", adapter.DeviceName, suffix);
        return await handler(this.HttpContext, suffix);
    }
}