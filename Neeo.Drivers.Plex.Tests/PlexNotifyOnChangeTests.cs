using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Neeo.Drivers.Plex.Tests;

public sealed class PlexNotifyOnChangeTests
{
    [Fact]
    public void The_initial_value_and_repeated_values_are_not_sent()
    {
        Subject<int> values = new();
        List<int> sent = [];

        using IDisposable subscription = PlexDeviceProviderBase.NotifyOnChange(values, new Subject<Unit>(), (value, _) => { sent.Add(value); return Task.CompletedTask; }, new RecordingLogger(), "test");
        values.OnNext(0);
        values.OnNext(0);
        values.OnNext(1);
        values.OnNext(1);
        values.OnNext(2);

        Assert.Equal([1, 2], sent);
    }

    [Fact]
    public void A_failed_send_is_logged_and_later_values_are_still_sent()
    {
        Subject<int> values = new();
        List<int> sent = [];
        RecordingLogger logger = new();

        using IDisposable subscription = PlexDeviceProviderBase.NotifyOnChange(
            values,
            new Subject<Unit>(),
            (value, _) => value == 2 ? throw new InvalidOperationException("Brain unreachable") : Record(value),
            logger,
            "test"
        );
        values.OnNext(0);
        values.OnNext(1);
        values.OnNext(2);
        values.OnNext(3);

        Assert.Equal([1, 3], sent);
        Assert.Contains(logger.Entries, static entry => entry.Level == LogLevel.Warning);

        Task Record(int value)
        {
            sent.Add(value);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task A_faulted_send_task_is_logged_and_later_values_are_still_sent()
    {
        Subject<int> values = new();
        List<int> sent = [];

        using IDisposable subscription = PlexDeviceProviderBase.NotifyOnChange(
            values,
            new Subject<Unit>(),
            async (value, _) =>
            {
                await Task.Yield();
                if (value == 2)
                {
                    throw new HttpRequestException("Brain unreachable");
                }
                lock (sent)
                {
                    sent.Add(value);
                }
            },
            new RecordingLogger(),
            "test"
        );
        values.OnNext(0);
        values.OnNext(1);
        await PlexNotifyOnChangeTests.WaitForAsync(() => sent.Count == 1);
        values.OnNext(2);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        values.OnNext(3);
        await PlexNotifyOnChangeTests.WaitForAsync(() => sent.Count == 2);

        Assert.Equal([1, 3], sent);
    }

    [Fact]
    public void An_error_in_the_value_stream_is_logged_instead_of_left_unhandled()
    {
        Subject<int> values = new();
        RecordingLogger logger = new();

        using IDisposable subscription = PlexDeviceProviderBase.NotifyOnChange(values, new Subject<Unit>(), (_, _) => Task.CompletedTask, logger, "test");
        values.OnNext(0);
        values.OnError(new InvalidOperationException("boom"));

        Assert.Contains(logger.Entries, static entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public void Nothing_is_sent_once_the_server_is_disposed()
    {
        Subject<int> values = new();
        Subject<Unit> disposed = new();
        List<int> sent = [];

        using IDisposable subscription = PlexDeviceProviderBase.NotifyOnChange(values, disposed, (value, _) => { sent.Add(value); return Task.CompletedTask; }, new RecordingLogger(), "test");
        values.OnNext(0);
        values.OnNext(1);
        disposed.OnNext(Unit.Default);
        values.OnNext(2);

        Assert.Equal([1], sent);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        for (int i = 0; i < 200 && !condition(); i++)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
        Assert.True(condition(), "Timed out waiting for the expected notifications.");
    }
}
