using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Neeo.Sdk.Rest;
using Neeo.Sdk.Rest.Endpoints;
using Xunit;

namespace Neeo.Sdk.Tests.Rest;

public sealed class SecureEndpointsTests
{
    [Fact]
    public async Task GetPublicKey_should_respond_with_the_public_key_under_the_publickey_property()
    {
        Mock<IPgpEncryption> mockPgpEncryption = new(MockBehavior.Strict);
        mockPgpEncryption.Setup(pgp => pgp.PublicKeyText).Returns(Constants.PublicKey);
        await using EndpointTestHost host = await EndpointTestHost.StartAsync(
            services => services.AddSingleton(mockPgpEncryption.Object),
            endpoints => endpoints.MapSecureEndpoints(),
            TestContext.Current.CancellationToken
        );

        using HttpResponseMessage response = await host.Client.GetAsync("/secure/pubkey", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(Constants.PublicKey, json.RootElement.GetProperty("publickey").GetString());
    }

    private static class Constants
    {
        public const string PublicKey = "-----BEGIN PGP PUBLIC KEY BLOCK-----";
    }
}
