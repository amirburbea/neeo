using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Neeo.Api.Devices;
using Neeo.Api.Devices.Components;

namespace Neeo.Api.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed class DeviceController : ControllerBase
{
    private readonly IDeviceDatabase _database;

    public DeviceController(IDeviceDatabase database) => this._database = database;

    [HttpGet("/{adapterId}/registered")]
    public async Task<ActionResult<IsRegisteredResponse>> GetIsRegistered(string adapterId)
    {
        IDeviceAdapter adapter = await this._database.GetAdapterAsync(adapterId);
        ICapabilityHandler? handler = adapter.GetCapabilityHandler(ComponentType.Registration);
        if (handler == null)
        {
            throw new();
        }
        bool registered = (bool)await handler.Controller.GetValueAsync(adapter.AdapterName);
        return new IsRegisteredResponse(registered);
    }

    [HttpPost("/{adapterId}/register")]
    public async Task<ActionResult<object>> Register(string adapterId, [FromBody] JsonElement credentials)
    {
        IDeviceAdapter adapter = await this._database.GetAdapterAsync(adapterId);
        ICapabilityHandler? handler = adapter.GetCapabilityHandler(ComponentType.Registration);
        if (handler == null)
        {
            throw new();
        }
        return await handler.Controller.ExecuteAsync(adapter.AdapterName, credentials);
    }

    public record struct IsRegisteredResponse(bool Registered);
}