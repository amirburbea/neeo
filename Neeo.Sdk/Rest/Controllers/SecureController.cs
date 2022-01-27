using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Neeo.Sdk.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed class SecureController : ControllerBase
{
    private readonly IPgpUtility _pgpKeys;

    public SecureController(IPgpUtility pgpKeys) => this._pgpKeys = pgpKeys;

    [HttpGet("pubkey")]
    public ActionResult<PublicKeyResult> GetPublicKey() => this.Ok(new PublicKeyResult(this._pgpKeys.ArmoredPublicKey));

    public record struct PublicKeyResult([property: JsonPropertyName("publickey")] string PublicKey);
}