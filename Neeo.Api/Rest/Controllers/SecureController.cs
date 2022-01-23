using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Neeo.Api.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed class SecureController : ControllerBase
{
    private readonly PgpComponents _pgpComponents;

    public SecureController(PgpComponents pgpComponents) => this._pgpComponents = pgpComponents;

    [HttpGet("pubkey")]
    public ActionResult<PublicKeyResult> GetPublicKey() => new PublicKeyResult(this._pgpComponents.PublicKeyArmored);

    public record struct PublicKeyResult([property: JsonPropertyName("publickey")] string PublicKey);
}