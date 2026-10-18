using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Neeo.Sdk.Rest.Endpoints;

internal static class SecureEndpoints
{
    public static void MapSecureEndpoints(this IEndpointRouteBuilder endpoints) => endpoints.MapGet("secure/pubkey", SecureEndpoints.GetPublicKey);

    private static PgpPublicKeyResponse GetPublicKey(IPgpEncryption pgpEncryption) => new(pgpEncryption.PublicKeyText);

    public readonly record struct PgpPublicKeyResponse(
        [property: JsonPropertyName("publickey")] string PublicKey
    );
}
