using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Drivers.Kodi;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Drivers.Kodi.Tests;

public class KodiClientManagerTests
{
    private static KodiClientManager CreateManager()
    {
        IConfiguration config = new ConfigurationBuilder().Build();
        Mock<ISettingsStore<KodiClientManager>> mockSettings = new();
        Mock<IHttpClientFactory> mockHttp = new();
        return new KodiClientManager(
            NullLogger<KodiClient>.Instance,
            config,
            mockSettings.Object,
            mockHttp.Object
        );
    }

    [Fact]
    public void RegisterImageUrl_ProducesShortDeterministicToken()
    {
        using KodiClientManager manager = KodiClientManagerTests.CreateManager();
        string rawUrl = "http://192.168.253.26:8080/vfs/image%3A%2F%2Fhttps%253A%252F%252Fimage.tmdb.org%252Ft%252Fp%252Foriginal%252Fsome_very_long_path.png%2F";

        string token1 = manager.RegisterImageUrl(rawUrl);
        string token2 = manager.RegisterImageUrl(rawUrl);

        Assert.Equal(12, token1.Length);
        Assert.Equal(token1, token2);
    }

    [Fact]
    public void ResolveImageUrl_ReturnsOriginalUrlForRegisteredToken()
    {
        using KodiClientManager manager = KodiClientManagerTests.CreateManager();
        string rawUrl = "http://192.168.253.26:8080/vfs/image%3A%2F%2Fmovie.jpg";

        string token = manager.RegisterImageUrl(rawUrl);
        string? resolved = manager.ResolveImageUrl(token);

        Assert.Equal(rawUrl, resolved);
    }

    [Fact]
    public void ResolveImageUrl_ReturnsNullForUnknownToken()
    {
        using KodiClientManager manager = KodiClientManagerTests.CreateManager();

        string? resolved = manager.ResolveImageUrl("nonexistent12");

        Assert.Null(resolved);
    }
}
