using Microsoft.AspNetCore.Mvc;

namespace Neeo.Sdk.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed class SecureController : ControllerBase
{
    private readonly PgpPublicKeyResponse _publicKeyResponse;

    public SecureController(PgpPublicKeyResponse publicKeyResponse) => this._publicKeyResponse = publicKeyResponse;

    [HttpGet("pubkey")]
    public ActionResult<PgpPublicKeyResponse> GetPublicKey() => this._publicKeyResponse;
}