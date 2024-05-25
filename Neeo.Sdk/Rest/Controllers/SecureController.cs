using Microsoft.AspNetCore.Mvc;

namespace Neeo.Sdk.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed class SecureController(PgpPublicKeyResponse publicKeyResponse) : ControllerBase
{
    [HttpGet("pubkey")]
    public ActionResult<PgpPublicKeyResponse> GetPublicKey() => publicKeyResponse;
}
