using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex.Tests;

internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    private int _requestCount;

    public int RequestCount => Volatile.Read(ref this._requestCount);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref this._requestCount);
        return respond(request, cancellationToken);
    }
}

internal sealed class FakeTokenStore(string? authToken = default) : IPlexTokenStore
{
    public string? AuthToken { get; set; } = authToken;

    public string ClientIdentifier => "test-client";
}

internal sealed class FakeSettingsStore : Dictionary<string, string>, ISettingsStore
{
    public FakeSettingsStore()
        : base(StringComparer.Ordinal)
    {
    }

    public string? Get(string key, string? defaultValue = default) => this.TryGetValue(key, out string? value) ? value : defaultValue;

    public void Update(Action<IDictionary<string, string>> action) => action(this);
}

internal sealed class RecordingLogger : ILogger
{
    private readonly List<(LogLevel Level, string Message)> _entries = [];
    private readonly Lock _lock = new();

    public IReadOnlyList<(LogLevel Level, string Message)> Entries
    {
        get
        {
            lock (this._lock)
            {
                return [.. this._entries];
            }
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (this._lock)
        {
            this._entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
