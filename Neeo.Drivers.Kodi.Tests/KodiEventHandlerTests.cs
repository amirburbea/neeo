using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Neeo.Drivers.Kodi.Models;
using Neeo.Sdk.Devices;
using Neeo.Sdk.Utilities;
using Xunit;

namespace Neeo.Drivers.Kodi.Tests;

/// <summary>
/// The connected handler shows a "Connected to ..." toast over an RPC that fails when the client has just dropped, so it must
/// handle its own failures. The notifier does not throw when the Brain is unreachable, so the others need no such guard.
/// </summary>
public sealed class KodiEventHandlerTests
{
    private static readonly PlayerState _playing = new(PlayState.Playing, "Title", "Description", "image://cover");

    [Fact]
    public async Task Connected_handling_does_not_throw_when_the_client_is_not_connected_to_show_the_notification()
    {
        // A client that never connected (or dropped right after connecting) fails the "Connected to ..." toast.
        RecordingLogger<KodiRemoteDeviceProvider> logger = new();
        using KodiRemoteDeviceProvider provider = KodiEventHandlerTests.CreateProvider(logger);
        using KodiClient client = KodiEventHandlerTests.CreateClient();

        await provider.HandleClientConnectedAsync(client);

        Assert.Contains(logger.Entries, static entry => entry.Level == LogLevel.Warning && entry.Message.Contains("connected handling"));
    }

    [Fact]
    public async Task Player_state_handling_sends_the_sensors_when_notifications_succeed()
    {
        Mock<IDeviceNotifier> notifier = new();
        notifier.Setup(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        notifier.Setup(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        using KodiRemoteDeviceProvider provider = KodiEventHandlerTests.CreateProvider(new RecordingLogger<KodiRemoteDeviceProvider>());
        using KodiClient client = KodiEventHandlerTests.CreateClient();
        provider.Notifier = notifier.Object;

        await provider.HandlePlayerStateChangedAsync(client, KodiEventHandlerTests._playing with { PlayState = PlayState.Paused });

        notifier.Verify(n => n.NotifyAsync("PLAYING_SENSOR", false, client.DeviceId, It.IsAny<CancellationToken>()), Times.Once);
        notifier.Verify(n => n.NotifyAsync("DESCRIPTION_SENSOR", "Description", client.DeviceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Connected_handling_sends_a_power_notification()
    {
        Mock<IDeviceNotifier> notifier = new();
        using KodiRemoteDeviceProvider provider = KodiEventHandlerTests.CreateProvider(new RecordingLogger<KodiRemoteDeviceProvider>());
        using KodiClient client = KodiEventHandlerTests.CreateClient();
        provider.Notifier = notifier.Object;

        await provider.HandleClientConnectedAsync(client);

        notifier.Verify(n => n.NotifyPowerStateAsync(true, client.DeviceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Disconnected_handling_sends_a_power_notification()
    {
        Mock<IDeviceNotifier> notifier = new();
        using KodiRemoteDeviceProvider provider = KodiEventHandlerTests.CreateProvider(new RecordingLogger<KodiRemoteDeviceProvider>());
        using KodiClient client = KodiEventHandlerTests.CreateClient();
        provider.Notifier = notifier.Object;

        await provider.HandleClientDisconnectedAsync(client);

        notifier.Verify(n => n.NotifyPowerStateAsync(false, client.DeviceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static KodiClient CreateClient() => new("Kodi", IPAddress.Loopback, 8080, new RecordingLogger());

    private static KodiRemoteDeviceProvider CreateProvider(ILogger<KodiRemoteDeviceProvider> logger) => new(
        new(
            NullLogger<KodiClient>.Instance,
            new ConfigurationBuilder().Build(),
            Mock.Of<ISettingsStore<KodiClientManager>>(),
            Mock.Of<IHttpClientFactory>(),
            Mock.Of<IKodiHostLocator>()
        ),
        logger
    );
}

internal sealed class RecordingLogger<T> : RecordingLogger, ILogger<T>
{
}

internal class RecordingLogger : ILogger
{
    private readonly System.Collections.Generic.List<(LogLevel Level, string Message)> _entries = [];
    private readonly Lock _lock = new();

    public System.Collections.Generic.IReadOnlyList<(LogLevel Level, string Message)> Entries
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
