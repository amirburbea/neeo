using System.Text.Json.Serialization;

namespace Neeo.Sdk.Rest;

internal sealed record class PgpPublicKeyResponse([property: JsonPropertyName("publickey")] string PublicKey);