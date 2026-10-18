using System.Text.Json;
using System.Text.Json.Serialization;
using Neeo.Sdk.Tests.Devices.Features;
using Neeo.Sdk.Tests.Utilities;

namespace Neeo.Sdk.Tests;

/// <summary>
/// Source-generated JSON metadata for the types used by tests that exercise the SDK's <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/>-based APIs.
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(RegistrationFeatureTests.TestCredentials))]
[JsonSerializable(typeof(JsonWebSocketTests.TestMessage))]
internal sealed partial class TestJsonContext : JsonSerializerContext;
