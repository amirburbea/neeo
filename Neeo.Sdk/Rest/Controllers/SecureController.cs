using Microsoft.AspNetCore.Mvc;

namespace Neeo.Sdk.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed class SecureController : ControllerBase
{
    private readonly PgpPublicKeyResponse _publicKey;

    public SecureController(PgpPublicKeyResponse publicKey) => this._publicKey = publicKey;

    [HttpGet("pubkey")]
    public ActionResult<PgpPublicKeyResponse> GetPublicKey() => this._publicKey;
}