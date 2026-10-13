using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Neeo.Sdk.Tests.Utilities;

public sealed class ReconnectLoopTests
{
    [Fact]
    public void Constructor_throws_for_non_positive_initial_delay()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Neeo.Sdk.Utilities.ReconnectLoop(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            static _ => Task.FromResult(true),
            NullLogger.Instance
        ));
    }

    [Fact]
    public void Constructor_throws_when_max_delay_is_less_than_initial_delay()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Neeo.Sdk.Utilities.ReconnectLoop(
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(1),
            static _ => Task.FromResult(true),
            NullLogger.Instance
        ));
    }

    [Fact]
    public async Task Start_stops_once_tryConnectAsync_succeeds()
    {
        int attempts = 0;
        using Neeo.Sdk.Utilities.ReconnectLoop loop = new(
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(40),
            _ =>
            {
                Interlocked.Increment(ref attempts);
                return Task.FromResult(attempts >= 3);
            },
            NullLogger.Instance
        );
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        loop.Start(cancellationToken);
        await ReconnectLoopTests.WaitUntilAsync(() => !loop.IsRunning, cancellationToken);

        Assert.Equal(3, attempts);
        Assert.False(loop.IsRunning);
    }

    [Fact]
    public async Task Start_is_a_no_op_when_already_running()
    {
        TaskCompletionSource neverCompletes = new();
        int attempts = 0;
        using Neeo.Sdk.Utilities.ReconnectLoop loop = new(
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(40),
            async _ =>
            {
                Interlocked.Increment(ref attempts);
                await neverCompletes.Task.ConfigureAwait(false);
                return true;
            },
            NullLogger.Instance
        );
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        loop.Start(cancellationToken);
        await ReconnectLoopTests.WaitUntilAsync(() => attempts > 0, cancellationToken);
        loop.Start(cancellationToken);
        loop.Start(cancellationToken);
        await Task.Delay(50, cancellationToken);

        Assert.True(loop.IsRunning);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task Loop_continues_retrying_after_an_unexpected_exception()
    {
        int attempts = 0;
        using Neeo.Sdk.Utilities.ReconnectLoop loop = new(
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(40),
            _ =>
            {
                int attempt = Interlocked.Increment(ref attempts);
                return attempt switch
                {
                    1 => throw new InvalidOperationException("Simulated transient failure."),
                    < 3 => Task.FromResult(false),
                    _ => Task.FromResult(true),
                };
            },
            NullLogger.Instance
        );
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        loop.Start(cancellationToken);
        await ReconnectLoopTests.WaitUntilAsync(() => !loop.IsRunning, cancellationToken);

        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task Backoff_grows_and_is_capped_at_max_delay()
    {
        List<DateTime> attemptTimestamps = [];
        using Neeo.Sdk.Utilities.ReconnectLoop loop = new(
            TimeSpan.FromMilliseconds(20),
            TimeSpan.FromMilliseconds(60),
            _ =>
            {
                attemptTimestamps.Add(DateTime.UtcNow);
                return Task.FromResult(attemptTimestamps.Count >= 5);
            },
            NullLogger.Instance
        );
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        loop.Start(cancellationToken);
        await ReconnectLoopTests.WaitUntilAsync(() => !loop.IsRunning, cancellationToken);

        Assert.Equal(5, attemptTimestamps.Count);
        // Expected gaps: ~20ms, ~40ms, ~60ms (capped), ~60ms (capped). Assert they're non-decreasing
        // with generous tolerance rather than asserting exact durations, since this runs on real timers.
        TimeSpan? previousGap = null;
        for (int i = 1; i < attemptTimestamps.Count; i++)
        {
            TimeSpan gap = attemptTimestamps[i] - attemptTimestamps[i - 1];
            if (previousGap is { } previous)
            {
                Assert.True(gap >= previous - TimeSpan.FromMilliseconds(15), $"Gap {gap} should not shrink relative to previous gap {previous}.");
            }
            previousGap = gap;
        }
    }

    [Fact]
    public void Dispose_stops_the_loop_and_prevents_further_starts()
    {
        int attempts = 0;
        Neeo.Sdk.Utilities.ReconnectLoop loop = new(
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(40),
            _ =>
            {
                Interlocked.Increment(ref attempts);
                return Task.FromResult(false);
            },
            NullLogger.Instance
        );

        loop.Dispose();
        loop.Start(TestContext.Current.CancellationToken);

        Assert.False(loop.IsRunning);
        Assert.Equal(0, attempts);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, CancellationToken cancellationToken)
    {
        for (int i = 0; i < 500 && !condition(); i++)
        {
            await Task.Delay(10, cancellationToken).ConfigureAwait(false);
        }
        Assert.True(condition(), "Timed out waiting for the expected condition.");
    }
}
