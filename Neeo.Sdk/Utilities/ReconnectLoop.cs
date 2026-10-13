using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// A reusable background reconnect loop: repeatedly invokes a connect attempt with exponential
/// backoff (capped at a maximum delay) until it succeeds or the loop is disposed. Intended for
/// device drivers that need to recover from a dropped connection (MQTT, WebSocket, etc.) without
/// each driver reimplementing its own retry scheduling.
/// </summary>
public sealed class ReconnectLoop : IDisposable
{
    private readonly TimeSpan _initialDelay;
    private readonly ILogger _logger;
    private readonly TimeSpan _maxDelay;
    private readonly Lock _lock = new();
    private readonly Func<CancellationToken, Task<bool>> _tryConnectAsync;
    private bool _isDisposed;
    private PeriodicTimer? _timer;

    /// <summary>
    /// Creates a new <see cref="ReconnectLoop"/>.
    /// </summary>
    /// <param name="initialDelay">The delay before the first retry attempt, and the starting point for backoff after each new call to <see cref="Start"/>.</param>
    /// <param name="maxDelay">The maximum delay between retry attempts once backoff has grown to this point.</param>
    /// <param name="tryConnectAsync">Attempts a single connection, returning <see langword="true"/> on success. The loop stops once this returns <see langword="true"/>.</param>
    /// <param name="logger">Logger used to report unexpected exceptions from <paramref name="tryConnectAsync"/> (the loop itself never dies silently - it logs and keeps retrying).</param>
    public ReconnectLoop(TimeSpan initialDelay, TimeSpan maxDelay, Func<CancellationToken, Task<bool>> tryConnectAsync, ILogger logger)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(initialDelay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDelay, initialDelay);
        (this._initialDelay, this._maxDelay) = (initialDelay, maxDelay);
        this._tryConnectAsync = tryConnectAsync ?? throw new ArgumentNullException(nameof(tryConnectAsync));
        this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a value indicating whether the loop is currently running.
    /// </summary>
    public bool IsRunning => this._timer is not null;

    /// <summary>
    /// Stops the loop (if running) and marks it as disposed - it can not be started again afterward.
    /// </summary>
    public void Dispose()
    {
        using (this._lock.EnterScope())
        {
            this._isDisposed = true;
            this.StopTimer();
        }
    }

    /// <summary>
    /// Starts the reconnect loop, unless it is already running or has been disposed (in which case this
    /// is a no-op) - only one reconnect cycle can be in flight at a time. Backoff always restarts from
    /// <see langword="initialDelay"/> for a new cycle.
    /// </summary>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    public void Start(CancellationToken cancellationToken = default)
    {
        PeriodicTimer timer;
        using (this._lock.EnterScope())
        {
            if (this._isDisposed || this._timer is not null)
            {
                return;
            }
            this._timer = timer = new(this._initialDelay);
        }
        _ = Task.Run(() => this.RunAsync(timer, cancellationToken), cancellationToken);
    }

    private async Task RunAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            TimeSpan delay = this._initialDelay;
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                bool connected;
                try
                {
                    connected = await this._tryConnectAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    this._logger.LogWarning(e, "Reconnect attempt failed unexpectedly.");
                    connected = false;
                }
                if (connected)
                {
                    return;
                }
                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, this._maxDelay.Ticks));
                timer.Period = delay;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during cancellation/disposal.
        }
        finally
        {
            using (this._lock.EnterScope())
            {
                if (ReferenceEquals(this._timer, timer))
                {
                    this.StopTimer();
                }
            }
        }
    }

    private void StopTimer()
    {
        this._timer?.Dispose();
        this._timer = null;
    }
}
