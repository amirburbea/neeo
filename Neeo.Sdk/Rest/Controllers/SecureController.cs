using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Neeo.Sdk.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed class SecureController(IPgpEncryption pgpEncryption) : ControllerBase
{
    [HttpGet("pubkey")]
    public ActionResult<PgpPublicKeyResponse> GetPublicKey()
    {
        pgpEncryption.RotateKeys();
        return new PgpPublicKeyResponse(pgpEncryption.PublicKeyText);
    }

    public readonly record struct PgpPublicKeyResponse(
        [property: JsonPropertyName("publickey")] string PublicKey
    );
}
