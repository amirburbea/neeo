using Microsoft.AspNetCore.Mvc;

namespace Neeo.Sdk.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed class SecureController : ControllerBase
{
    private readonly PublicKeyResponse _publicKey;

    public SecureController(PublicKeyResponse publicKey) => this._publicKey = publicKey;

    [HttpGet("pubkey")]
    public ActionResult<PublicKeyResponse> GetPublicKey() => this._publicKey;
}