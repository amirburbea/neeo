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

        loop.Start(cancellationToken: cancellationToken);
        await ReconnectLoopTests.WaitUntilAsync(() => !loop.IsRunning, cancellationToken);

        Assert.Equal(3, attempts);
        Assert.False(loop.IsRunning);
    }

    [Fact]
    public async Task Attempt_token_is_cancelled_once_an_attempt_succeeds()
    {
        CancellationToken attemptToken = default;
        using Neeo.Sdk.Utilities.ReconnectLoop loop = new(
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(40),
            token =>
            {
                attemptToken = token;
                return Task.FromResult(true);
            },
            NullLogger.Instance
        );
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        loop.Start(cancellationToken: cancellationToken);
        await ReconnectLoopTests.WaitUntilAsync(() => !loop.IsRunning, cancellationToken);

        // Work that outlives the attempt (e.g. a session started on success) must not be bound to this token.
        Assert.True(attemptToken.IsCancellationRequested);
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

        loop.Start(cancellationToken: cancellationToken);
        await ReconnectLoopTests.WaitUntilAsync(() => attempts > 0, cancellationToken);
        loop.Start(cancellationToken: cancellationToken);
        loop.Start(cancellationToken: cancellationToken);
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

        loop.Start(cancellationToken: cancellationToken);
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

        loop.Start(cancellationToken: cancellationToken);
        await ReconnectLoopTests.WaitUntilAsync(() => !loop.IsRunning, cancellationToken);

        Assert.Equal(5, attemptTimestamps.Count);
        // Expected delays before attempts 2..5: 40ms, 60ms (capped), 60ms (capped), 60ms (capped).
        // Compare each gap against its scheduled target (with timer tolerance) rather than the previous
        // observed gap, since OS scheduling jitter on one capped tick does not carry over to the next.
        TimeSpan[] expectedDelays =
        [
            TimeSpan.FromMilliseconds(40),
            TimeSpan.FromMilliseconds(60),
            TimeSpan.FromMilliseconds(60),
            TimeSpan.FromMilliseconds(60),
        ];
        TimeSpan tolerance = TimeSpan.FromMilliseconds(15);
        for (int i = 1; i < attemptTimestamps.Count; i++)
        {
            TimeSpan gap = attemptTimestamps[i] - attemptTimestamps[i - 1];
            TimeSpan expected = expectedDelays[i - 1];
            Assert.True(gap >= expected - tolerance, $"Gap {gap} was shorter than expected delay {expected} (minus tolerance).");
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
        loop.Start(cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(loop.IsRunning);
        Assert.Equal(0, attempts);
    }

    [Fact]
    public async Task Start_with_an_already_cancelled_token_does_not_leave_the_loop_stuck_running()
    {
        int attempts = 0;
        using Neeo.Sdk.Utilities.ReconnectLoop loop = new(
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(40),
            _ =>
            {
                Interlocked.Increment(ref attempts);
                return Task.FromResult(true);
            },
            NullLogger.Instance
        );
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        loop.Start(cancellationToken: new CancellationToken(canceled: true));
        await ReconnectLoopTests.WaitUntilAsync(() => !loop.IsRunning, cancellationToken);
        Assert.Equal(0, attempts);

        // The loop can be started again afterward.
        loop.Start(cancellationToken: cancellationToken);
        await ReconnectLoopTests.WaitUntilAsync(() => attempts == 1 && !loop.IsRunning, cancellationToken);
    }

    [Fact]
    public async Task Dispose_cancels_a_connect_attempt_in_flight()
    {
        TaskCompletionSource<bool> attemptCancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Neeo.Sdk.Utilities.ReconnectLoop loop = new(
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(40),
            async token =>
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    attemptCancelled.TrySetResult(true);
                    throw;
                }
                return true;
            },
            NullLogger.Instance
        );
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        loop.Start(cancellationToken: cancellationToken);
        await Task.Delay(50, cancellationToken);
        loop.Dispose();

        Assert.True(await attemptCancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken));
        Assert.False(loop.IsRunning);
    }

    [Fact]
    public async Task Start_immediate_makes_the_first_attempt_without_waiting()
    {
        int attempts = 0;
        using Neeo.Sdk.Utilities.ReconnectLoop loop = new(
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5),
            _ =>
            {
                Interlocked.Increment(ref attempts);
                return Task.FromResult(true);
            },
            NullLogger.Instance
        );
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        loop.Start(immediate: true, cancellationToken: cancellationToken);
        await ReconnectLoopTests.WaitUntilAsync(() => !loop.IsRunning, cancellationToken);

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task TriggerNow_cuts_a_wait_short()
    {
        int attempts = 0;
        using Neeo.Sdk.Utilities.ReconnectLoop loop = new(
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5),
            _ => Task.FromResult(Interlocked.Increment(ref attempts) >= 1),
            NullLogger.Instance
        );
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        loop.Start(cancellationToken: cancellationToken);
        await Task.Delay(50, cancellationToken);
        Assert.Equal(0, attempts);
        loop.TriggerNow(cancellationToken);
        await ReconnectLoopTests.WaitUntilAsync(() => !loop.IsRunning, cancellationToken);

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task TriggerNow_starts_a_stopped_loop_immediately()
    {
        int attempts = 0;
        using Neeo.Sdk.Utilities.ReconnectLoop loop = new(
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5),
            _ =>
            {
                Interlocked.Increment(ref attempts);
                return Task.FromResult(true);
            },
            NullLogger.Instance
        );
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        loop.TriggerNow(cancellationToken);
        await ReconnectLoopTests.WaitUntilAsync(() => attempts == 1 && !loop.IsRunning, cancellationToken);
    }

    [Fact]
    public async Task TriggerNow_during_an_attempt_runs_another_attempt_without_waiting_and_restarts_backoff()
    {
        TaskCompletionSource firstAttemptStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirstAttempt = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int attempts = 0;
        using Neeo.Sdk.Utilities.ReconnectLoop loop = new(
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMinutes(5),
            async _ =>
            {
                if (Interlocked.Increment(ref attempts) == 1)
                {
                    firstAttemptStarted.TrySetResult();
                    await releaseFirstAttempt.Task.ConfigureAwait(false);
                    return false;
                }
                return true;
            },
            NullLogger.Instance
        );
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        loop.Start(immediate: true, cancellationToken: cancellationToken);
        await firstAttemptStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        loop.TriggerNow(cancellationToken);
        loop.TriggerNow(cancellationToken); // Coalesced with the first.
        releaseFirstAttempt.TrySetResult();
        await ReconnectLoopTests.WaitUntilAsync(() => !loop.IsRunning, cancellationToken);

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task Stop_cancels_the_cycle_and_the_loop_can_be_started_again()
    {
        int attempts = 0;
        using Neeo.Sdk.Utilities.ReconnectLoop loop = new(
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5),
            _ =>
            {
                Interlocked.Increment(ref attempts);
                return Task.FromResult(true);
            },
            NullLogger.Instance
        );
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        loop.Start(cancellationToken: cancellationToken);
        Assert.True(loop.IsRunning);
        loop.Stop();
        Assert.False(loop.IsRunning);
        await Task.Delay(50, cancellationToken);
        Assert.Equal(0, attempts);

        loop.Start(immediate: true, cancellationToken: cancellationToken);
        await ReconnectLoopTests.WaitUntilAsync(() => attempts == 1 && !loop.IsRunning, cancellationToken);
    }

    [Fact]
    public void TriggerNow_after_Dispose_is_a_no_op()
    {
        int attempts = 0;
        Neeo.Sdk.Utilities.ReconnectLoop loop = new(
            TimeSpan.FromMilliseconds(10),
            TimeSpan.FromMilliseconds(40),
            _ =>
            {
                Interlocked.Increment(ref attempts);
                return Task.FromResult(true);
            },
            NullLogger.Instance
        );

        loop.Dispose();
        loop.TriggerNow(TestContext.Current.CancellationToken);

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
