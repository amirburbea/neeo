using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Neeo.Sdk.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed class SecureController : ControllerBase
{
    private readonly IPgpService _pgp;

    public SecureController(IPgpService pgp) => this._pgp = pgp;

    [HttpGet("pubkey")]
    public ActionResult<PublicKeyData> GetPublicKeyData() => this.Serialize(new PublicKeyData(this._pgp.ArmoredPublicKey));

    public record struct PublicKeyData([property: JsonPropertyName("publickey")] string ArmoredPublicKey);
}