using System.Text.Json.Serialization;

namespace Neeo.Sdk.Rest;

public sealed record class PublicKeyResponse([property: JsonPropertyName("publickey")] string PublicKey);