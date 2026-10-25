using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Drivers.Kodi.Tests;

public sealed class KodiDeviceProviderTests
{
    private const string PlexCoverArt = "image://https%3a%2f%2f192-168-253-3.plex.direct%3a32400%2fphoto%2f%3a%2ftranscode%3fX-Plex-Token%3dsecret%2f";
    private const string UriPrefix = "http://192.168.253.1:6336/device/adapter/custom/";

    [Fact]
    public void GetThumbnailUrl_before_the_uri_prefix_is_known_returns_the_placeholder_not_the_raw_url()
    {
        using KodiClientManager manager = KodiDeviceProviderTests.CreateManager();
        KodiRemoteDeviceProvider provider = new(manager, NullLogger<KodiRemoteDeviceProvider>.Instance);
        using KodiClient client = new("Kodi", IPAddress.Loopback, 8080, NullLogger.Instance);

        string url = provider.GetThumbnailUrl(client, PlexCoverArt);

        Assert.Equal(Images.Kodi, url);
    }

    [Fact]
    public void GetThumbnailUrl_after_the_uri_prefix_is_known_returns_a_short_url_without_the_source_address()
    {
        using KodiClientManager manager = KodiDeviceProviderTests.CreateManager();
        KodiRemoteDeviceProvider provider = new(manager, NullLogger<KodiRemoteDeviceProvider>.Instance);
        provider.DeviceBuilder.BuildAdapter().UriPrefixCallback!(KodiDeviceProviderTests.UriPrefix);
        using KodiClient client = new("Kodi", IPAddress.Loopback, 8080, NullLogger.Instance);

        string url = provider.GetThumbnailUrl(client, PlexCoverArt);

        Assert.StartsWith($"{KodiDeviceProviderTests.UriPrefix}thumbnail/480/", url);
        Assert.DoesNotContain("Plex", url);
        Assert.DoesNotContain("secret", url);
    }

    private static KodiClientManager CreateManager() => new(
        NullLogger<KodiClient>.Instance,
        new ConfigurationBuilder().Build(),
        Mock.Of<ISettingsStore<KodiClientManager>>(),
        Mock.Of<IHttpClientFactory>(),
        Mock.Of<IKodiHostLocator>()
    );
}
