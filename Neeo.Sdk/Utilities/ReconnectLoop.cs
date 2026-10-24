using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// A reusable background reconnect loop: repeatedly invokes a connect attempt with exponential
/// backoff (capped at a maximum delay) until it succeeds, or the loop is stopped or disposed. Intended for
/// device drivers that need to recover from a dropped connection (MQTT, WebSocket, etc.) without
/// each driver reimplementing its own retry scheduling.
/// <para/>
/// A driver typically keeps one loop per connection for the connection's lifetime: <see cref="Start"/> it when a
/// connection attempt fails or a connection drops, <see cref="Start"/> it with <c>immediate: true</c> when there is reason
/// to believe the device is reachable again (a user action, a Wake-on-LAN packet), and <see cref="Dispose"/> it with the
/// connection.
/// </summary>
public sealed class ReconnectLoop : IDisposable
{
    private readonly TimeSpan _initialDelay;
    private readonly ILogger _logger;
    private readonly TimeSpan _maxDelay;
    private readonly Lock _lock = new();
    private readonly Func<CancellationToken, Task<bool>> _tryConnectAsync;
    private CancellationTokenSource? _cycleCancellation;
    private bool _isDisposed;
    private CancellationTokenSource? _wait;
    private bool _isTriggered;

    /// <summary>
    /// Creates a new <see cref="ReconnectLoop"/>.
    /// </summary>
    /// <param name="initialDelay">The delay before the first retry attempt, and the starting point for backoff after each new call to <see cref="Start"/>.</param>
    /// <param name="maxDelay">The maximum delay between retry attempts once backoff has grown to this point.</param>
    /// <param name="tryConnectAsync">
    /// Attempts a single connection, returning <see langword="true"/> on success. The loop stops once this returns <see langword="true"/>.
    /// The token it is given is scoped to the reconnect cycle and is cancelled once the cycle ends (including on success), so work
    /// that should outlive the attempt (such as a session started on success) must not be bound to it.
    /// </param>
    /// <param name="logger">Logger used to report unexpected exceptions from <paramref name="tryConnectAsync"/> (the loop itself never dies silently - it logs and keeps retrying).</param>
    public ReconnectLoop(TimeSpan initialDelay, TimeSpan maxDelay, Func<CancellationToken, Task<bool>> tryConnectAsync, ILogger logger)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(initialDelay, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDelay, initialDelay);
        (this._initialDelay, this._maxDelay) = (initialDelay, maxDelay);
        (this._tryConnectAsync, this._logger) = (tryConnectAsync, logger);
    }

    internal bool IsRunning
    {
        get
        {
            using (this._lock.EnterScope())
            {
                return this._cycleCancellation is not null;
            }
        }
    }

    /// <summary>
    /// Stops the loop (if running), cancelling any connect attempt in flight, and marks it as disposed - it can not be
    /// started again afterward.
    /// </summary>
    public void Dispose()
    {
        using (this._lock.EnterScope())
        {
            this._isDisposed = true;
            this.StopCycle();
        }
    }

    /// <summary>
    /// Starts the reconnect loop, unless it has been disposed (in which case this is a no-op) - only one reconnect cycle can
    /// be in flight at a time. Backoff always restarts from <see langword="initialDelay"/> for a new cycle.
    /// <para/>
    /// If the loop is already running, this does nothing, unless <paramref name="immediate"/> is <see langword="true"/>: then
    /// the backoff restarts from <see langword="initialDelay"/> and an attempt is made as soon as possible. If the loop is
    /// waiting, the wait ends now; if an attempt is in flight, another follows it without waiting (unless it succeeds).
    /// Repeated immediate calls before the attempt starts are coalesced.
    /// </summary>
    /// <param name="immediate">
    /// <see langword="true"/> to attempt a connection right away (see above) rather than after <see langword="initialDelay"/>.
    /// </param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests, used only if this starts the loop.</param>
    public void Start(bool immediate = false, CancellationToken cancellationToken = default)
    {
        CancellationTokenSource cycle;
        using (this._lock.EnterScope())
        {
            if (this._isDisposed)
            {
                return;
            }
            if (this._cycleCancellation is not null)
            {
                if (immediate)
                {
                    this._isTriggered = true;
                    this._wait?.Cancel();
                }
                return;
            }
            this._cycleCancellation = cycle = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            this._isTriggered = immediate;
        }
        // Deliberately not passing the token to Task.Run: if it were already cancelled, RunAsync would never run, its
        // finally block would never end the cycle, and the loop would report IsRunning forever.
        _ = Task.Run(() => this.RunAsync(cycle, cycle.Token), CancellationToken.None);
    }

    private async Task RunAsync(CancellationTokenSource cycle, CancellationToken cancellationToken)
    {
        try
        {
            TimeSpan delay = this._initialDelay;
            while (true)
            {
                CancellationTokenSource? wait = null;
                bool isTriggered;
                using (this._lock.EnterScope())
                {
                    if (!ReferenceEquals(this._cycleCancellation, cycle))
                    {
                        return;
                    }
                    if (!(isTriggered = this._isTriggered))
                    {
                        this._wait = wait = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    }
                    this._isTriggered = false;
                }
                if (wait is not null)
                {
                    try
                    {
                        await Task.Delay(delay, wait.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        // Cut short by an immediate Start.
                        isTriggered = true;
                    }
                    finally
                    {
                        using (this._lock.EnterScope())
                        {
                            if (ReferenceEquals(this._wait, wait))
                            {
                                this._wait = null;
                            }
                            if (isTriggered)
                            {
                                // This attempt consumes the trigger that cut the wait short.
                                this._isTriggered = false;
                            }
                        }
                        wait.Dispose();
                    }
                }
                cancellationToken.ThrowIfCancellationRequested();
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
                // A triggered attempt restarts the backoff; otherwise it grows, capped at the maximum.
                delay = isTriggered ? this._initialDelay : TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, this._maxDelay.Ticks));
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during cancellation/stop/disposal.
        }
        finally
        {
            using (this._lock.EnterScope())
            {
                if (ReferenceEquals(this._cycleCancellation, cycle))
                {
                    this.StopCycle();
                }
            }
        }
    }

    private void StopCycle()
    {
        this._isTriggered = false;
        this._wait = null; // Disposed by RunAsync, which also observes the cycle cancellation below.
        this._cycleCancellation?.Cancel();
        this._cycleCancellation?.Dispose();
        this._cycleCancellation = null;
    }
}
