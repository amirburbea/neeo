using System;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

public interface IPlexTokenStore
{
    string? AuthToken { get; set; }

    string ClientIdentifier { get; }
}

[SettingsStore("plex.tokens.bin")]
internal sealed class PlexTokenStore : IPlexTokenStore
{
    private const string AuthTokenKey = "authToken";
    private const string ClientIdentifierKey = "clientIdentifier";

    private readonly ISettingsStore<PlexTokenStore> _settings;

    public PlexTokenStore(ISettingsStore<PlexTokenStore> settings)
    {
        this._settings = settings;
        if (settings.TryGetValue(ClientIdentifierKey, out string? clientId) && !string.IsNullOrEmpty(clientId))
        {
            this.ClientIdentifier = clientId;
            this.AuthToken = settings.Get(AuthTokenKey);
        }
        else
        {
            this.ClientIdentifier = Guid.NewGuid().ToString("N");
            settings[ClientIdentifierKey] = this.ClientIdentifier;
        }
    }

    public string? AuthToken { get; private set; }

    public string ClientIdentifier { get; }

    string? IPlexTokenStore.AuthToken
    {
        get => this.AuthToken;
        set
        {
            this.AuthToken = value;
            if (value is null)
            {
                this._settings.Remove(AuthTokenKey);
            }
            else
            {
                this._settings[AuthTokenKey] = value;
            }
        }
    }
}
