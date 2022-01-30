using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Neeo.Sdk.Utilities;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Neeo.Sdk.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed class SecureController : ControllerBase
{
    private readonly PgpPublicKey _publicKey;

    public SecureController(PgpKeyPair pgpKeys) => this._publicKey = pgpKeys.PublicKey;

    [HttpGet("pubkey")]
    public ActionResult<PublicKeyResponse> GetPublicKeyData() => this.Serialize(new PublicKeyResponse(PgpMethods.GetArmoredPublicKey(this._publicKey)));

    public record struct PublicKeyResponse([property: JsonPropertyName("publickey")] string ArmoredPublicKey);
}