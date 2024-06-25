using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Neeo.Sdk.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed partial class DeviceController(
    IDeviceDatabase database,
    IDynamicDeviceRegistry dynamicDeviceRegistry,
    PgpKeyPair pgpKeys,
    ILogger<DeviceController> logger
) : ControllerBase
{
}
