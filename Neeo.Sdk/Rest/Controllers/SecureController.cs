using System.IO;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace Neeo.Sdk.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed class SecureController : ControllerBase
{
    private readonly PgpPublicKey _publicKey;

    public SecureController(PgpKeyPair pgpKeys) => this._publicKey = pgpKeys.PublicKey;

    [HttpGet("pubkey")]
    public async Task<ActionResult<PublicKeyResponse>> GetPublicKey()
    {
        using MemoryStream outputStream = new();
        using (ArmoredOutputStream armoredStream = new(outputStream))
        {
            armoredStream.SetHeader(ArmoredOutputStream.HeaderVersion, default);
            this._publicKey.Encode(armoredStream);
        }
        outputStream.Seek(0L, SeekOrigin.Begin);
        using StreamReader reader = new(outputStream);
        return this.Ok(new PublicKeyResponse(await reader.ReadToEndAsync()));
    }

    public record struct PublicKeyResponse([property: JsonPropertyName("publickey")] string PublicKey);
}