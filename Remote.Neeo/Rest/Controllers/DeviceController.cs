using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Remote.Neeo.Devices;
using Remote.Neeo.Devices.Components;

namespace Remote.Neeo.Rest.Controllers;

    [ApiController, Route("[controller]")]
    internal sealed class DeviceController : ControllerBase
    {
        private readonly IDeviceDatabase _database;

        public DeviceController(IDeviceDatabase database) => this._database = database;

        [HttpGet("/{adapterId}/registered")]
        public async Task<ActionResult<IsRegisteredResponse>> GetIsRegistered(string adapterId)
        {
            IDeviceAdapter adapter = await this._database.GetAdapterAsync(adapterId);
            ICapabilityHandler? handler = adapter.GetHandler(ComponentType.Discovery);
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
            ICapabilityHandler? handler = adapter.GetHandler(ComponentType.Registration);
            if (handler == null)
            {
                throw new();
            }
        await handler.Controller.ExecuteAsync(adapter.AdapterName, credentials);
        }

        public readonly struct IsRegisteredResponse
        {
            public IsRegisteredResponse(bool registered) => this.Registered = registered;

            public bool Registered { get; }
        }
    }