using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Neeo.Api.Devices;

namespace Neeo.Api.Rest.Controllers;

internal partial class DeviceController
{
    [HttpGet("{adapter1}/subscribe/{deviceId}/{_}")]
    public async Task<ActionResult<SuccessResult>> SubscribeAsync(
        [ModelBinder(typeof(AdapterBinder))] IDeviceAdapter adapter, 
        string deviceId
    )
    {


        return (SuccessResult)false;
    }

    [HttpGet("{adapterName}/unsubscribe/{deviceId}")]
    public async Task<ActionResult<SuccessResult>> UnsubscribeAsync(
        string adapterName, 
        string deviceId
    )
    {
        return (SuccessResult)false;
    }




    
}

