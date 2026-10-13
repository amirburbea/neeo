using System;
using System.Security.Cryptography;
using System.Text;
using BitFaster.Caching.Lru;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// Thread-safe bounded LRU mapping of long URLs to deterministic 12-character SHA-256 hex tokens,
/// keeping proxied URLs well within the NEEO Brain's 255-character URI limit.
/// </summary>
public sealed class UrlTokenStore(int capacity = 256)
{
    private readonly ConcurrentLru<string, string> _tokens = new(capacity);

    /// <summary>
    /// Registers a URL in the store and returns its deterministic 12-character lowercase hex token.
    /// </summary>
    public string Register(string url)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);
        string token = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(url)))[..12].ToLowerInvariant();
        this._tokens.AddOrUpdate(token, url);
        return token;
    }

    /// <summary>
    /// Resolves a previously registered token back to its original URL, or <see langword="null"/> if not found.
    /// </summary>
    public string? Resolve(string token) => this._tokens.TryGet(token, out string? url) ? url : null;
}
