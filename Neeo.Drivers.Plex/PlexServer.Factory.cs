using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Neeo.Drivers.Plex;

partial class PlexServer
{
    public sealed class Factory(
        IHttpClientFactory httpClientFactory,
        IPlexSettingsManager settingsManager,
        IPlexServerDiscovery serverDiscovery,
        IPlexTokenStore tokenStore,
        ILoggerFactory loggerFactory
    ) : IPlexServerFactory, IDisposable
    {
        private readonly HttpClient _httpClient = Factory.CreateHttpClient(httpClientFactory, tokenStore);

        public void Dispose() => this._httpClient.Dispose();

        private static HttpClient CreateHttpClient(IHttpClientFactory httpClientFactory, IPlexTokenStore tokenStore)
        {
            HttpClient client = httpClientFactory.CreateClient("plex");
            client.DefaultRequestHeaders.Add("X-Plex-Token", tokenStore.AuthToken);
            client.DefaultRequestHeaders.Add("X-Plex-Client-Identifier", tokenStore.ClientIdentifier);
            return client;
        }

        public PlexServer Create(string machineIdentifier) => new(
            machineIdentifier,
            this._httpClient,
            settingsManager,
            serverDiscovery,
            tokenStore,
            loggerFactory.CreateLogger<PlexServer>()
        );

        IPlexServer IPlexServerFactory.Create(string machineIdentifier) => this.Create(machineIdentifier);
    }
}
