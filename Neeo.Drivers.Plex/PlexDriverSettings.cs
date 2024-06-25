using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

public interface IPlexDriverSettings
{
    string ClientIdentifier { get; }

    IDictionary<string, PlexServerSettings> Servers { get; }
}

internal sealed class PlexDriverSettings : ApplicationSettingsBase, IPlexDriverSettings, IHostedService
{
    private readonly Dictionary<string, PlexServerSettings> _servers = [];

    [UserScopedSetting]
    [SettingsSerializeAs(SettingsSerializeAs.String)]
    public string? ClientIdentifier
    {
        get => this[nameof(this.ClientIdentifier)] as string;
        set => this[nameof(this.ClientIdentifier)] = value;
    }

    string IPlexDriverSettings.ClientIdentifier => this.ClientIdentifier ?? string.Empty;

    [UserScopedSetting]
    [SettingsSerializeAs(SettingsSerializeAs.String)]
    public string? Servers
    {
        get => this[nameof(this.Servers)] as string;
        set => this[nameof(this.Servers)] = value;
    }

    IDictionary<string, PlexServerSettings> IPlexDriverSettings.Servers => this._servers;

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        this.CoalesceClientIdentifier();
        if (this.Servers is { Length: not 0 } json)
        {
            PlexDriverSettings.AddEntries(json, this._servers);
        }
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        this.Servers = JsonSerializer.Serialize(this._servers, JsonSerialization.WebOptions);
        this.Save();
        return Task.CompletedTask;
    }

    private static void AddEntries<TKey, TValue>(string json, Dictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        foreach ((TKey key, TValue value) in JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(json, JsonSerialization.WebOptions)!)
        {
            dictionary.Add(key, value);
        }
    }

    private void CoalesceClientIdentifier()
    {
        if (string.IsNullOrEmpty(this.ClientIdentifier))
        {
            this.ClientIdentifier = Guid.NewGuid().ToString();
        }
    }
}
