using System.IO;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Bcpg;

namespace Neeo.Sdk.Rest.Controllers;

[ApiController, Route("[controller]")]
internal sealed class SecureController : ControllerBase
{
    private readonly IPgpKeys _pgpKeys;

    public SecureController(IPgpKeys pgpKeys) => this._pgpKeys = pgpKeys;

    [HttpGet("pubkey")]
    public ActionResult<PublicKeyResult> GetPublicKey()
    {
        using MemoryStream outputStream = new();
        using (ArmoredOutputStream armoredStream = new(outputStream))
        {
            armoredStream.SetHeader(ArmoredOutputStream.HeaderVersion, null);
            this._pgpKeys.PublicKey.Encode(armoredStream);
        }
        outputStream.Seek(0L, SeekOrigin.Begin);
        StreamReader reader = new(outputStream);
        return new PublicKeyResult(reader.ReadToEnd());
    }

    public record struct PublicKeyResult([property: JsonPropertyName("publickey")] string PublicKey);
}