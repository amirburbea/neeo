using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;

namespace Neeo.Sdk.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed partial class DeviceController(
    IDeviceDatabase database,
    IDynamicDeviceRegistry dynamicDeviceRegistry,
    IPgpEncryption pgpEncryption,
    ILogger<DeviceController> logger
) : ControllerBase
{
    private ValueTask<IDeviceAdapter?> GetAdapterAsync(string adapterName, CancellationToken cancellationToken) => database.GetAdapterAsync(adapterName, cancellationToken);
}
