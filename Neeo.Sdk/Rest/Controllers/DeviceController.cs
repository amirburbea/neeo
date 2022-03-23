using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Devices;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Neeo.Sdk.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed partial class DeviceController : ControllerBase
{
    private readonly IDeviceDatabase _database;
    private readonly IDynamicDevices _dynamicDevices;
    private readonly ILogger<DeviceController> _logger;
    private readonly PgpPrivateKey _privateKey;

    public DeviceController(IDeviceDatabase database, IDynamicDevices dynamicDevices, PgpKeyPair pgpKeys, ILogger<DeviceController> logger)
    {
        (this._database, this._dynamicDevices, this._privateKey, this._logger) = (database, dynamicDevices, pgpKeys.PrivateKey, logger);
    }
}