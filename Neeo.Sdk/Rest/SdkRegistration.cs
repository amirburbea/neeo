using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Rest;

/// <summary>
/// Hosted service responsible for registering the integration server with the NEEO Brain
/// and maintaining registration via periodic heartbeat.
/// </summary>
internal sealed class SdkRegistration(
    IBrain brain,
    IApiClient client,
    ISdkEnvironment environment,
    ILogger<SdkRegistration> logger,
    TimeSpan? heartbeatInterval = default
) : IHostedService, IAsyncDisposable, IDisposable
{
    private readonly TimeSpan _heartbeatInterval = heartbeatInterval ?? TimeSpan.FromSeconds(30);
    private readonly CancellationTokenSource _stoppingCts = new();
    private Task? _heartbeatTask;
    private bool _isRegistered;

    /// <summary>
    /// Occurs when the confirmed registration state of the server changes.
    /// </summary>
    public event Action<bool>? RegistrationStateChanged;

    /// <summary>
    /// Gets a value indicating whether the integration server is currently confirmed as registered with the NEEO Brain.
    /// </summary>
    public bool IsRegistered => this._isRegistered;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            RegisterServerRequest request = new(environment.SdkAdapterName, environment.HostAddress);
            await client.PostAsync(BrainUrlPaths.RegisterServer, request, cancellationToken).ConfigureAwait(false);
            this.SetIsRegistered(true);
            logger.LogInformation("Server {Name} registered on {Brain} ({Address}).", environment.SdkAdapterName, brain.HostName, brain.ServiceEndPoint.Address);
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed to register with brain - {Content}.", e.Message);
            throw;
        }

        if (this._heartbeatInterval > TimeSpan.Zero)
        {
            this._heartbeatTask = Task.Run(() => this.HeartbeatLoopAsync(this._stoppingCts.Token));
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        this._stoppingCts.Cancel();
        if (this._heartbeatTask is { } task)
        {
            try
            {
                await task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
            catch (Exception e)
            {
                logger.LogDebug(e, "Error waiting for heartbeat task to stop.");
            }
        }

        try
        {
            UnregisterServerRequest request = new(environment.SdkAdapterName);
            await client.PostAsync(BrainUrlPaths.UnregisterServer, request, cancellationToken).ConfigureAwait(false);
            this.SetIsRegistered(false);
            logger.LogInformation("Server unregistered from {brain}.", brain.HostName);
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed to unregister with brain - {content}.", e.Message);
        }
    }

    public void Dispose()
    {
        this._stoppingCts.Cancel();
        this._stoppingCts.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        this._stoppingCts.Cancel();
        if (this._heartbeatTask is { } task)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }
        this._stoppingCts.Dispose();
    }

    private void SetIsRegistered(bool isRegistered)
    {
        if (this._isRegistered != isRegistered)
        {
            this._isRegistered = isRegistered;
            this.RegistrationStateChanged?.Invoke(isRegistered);
        }
    }

    private async Task HeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(this._heartbeatInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await this.CheckHealthAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task CheckHealthAsync(CancellationToken cancellationToken)
    {
        bool isAlive;
        try
        {
            isAlive = await client.IsAliveAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogDebug("Health check exception against Brain ({Address}): {Content}", brain.ServiceEndPoint.Address, e.Message);
            isAlive = false;
        }

        if (isAlive)
        {
            // If the server was marked unregistered (because connection was previously lost/rebooted), re-register now!
            if (!this._isRegistered)
            {
                try
                {
                    RegisterServerRequest request = new(environment.SdkAdapterName, environment.HostAddress);
                    bool registered = await client.PostAsync(BrainUrlPaths.RegisterServer, request, cancellationToken).ConfigureAwait(false);
                    if (registered)
                    {
                        logger.LogInformation("Server {Name} re-registered on {Brain} ({Address}) after connection restored.", environment.SdkAdapterName, brain.HostName, brain.ServiceEndPoint.Address);
                        this.SetIsRegistered(true);
                    }
                    else
                    {
                        logger.LogWarning("Registration renewal returned failure on {Brain} ({Address}). Will retry on next check...", brain.HostName, brain.ServiceEndPoint.Address);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception e)
                {
                    logger.LogWarning("Failed to re-register on {Brain} ({Address}): {Content}. Will retry on next check...", brain.HostName, brain.ServiceEndPoint.Address, e.Message);
                }
            }
            // If already registered and Brain is alive: DO NOTHING!
            // No registration POSTs, zero state mutations, zero overhead on the Brain's dual-core 512MB RAM!
        }
        else
        {
            // Brain is offline or rebooting
            if (this._isRegistered)
            {
                logger.LogWarning("Lost connection to NEEO Brain ({Address}). Will attempt to re-register once reachable...", brain.ServiceEndPoint.Address);
                this.SetIsRegistered(false);
            }
            else
            {
                logger.LogDebug("NEEO Brain ({Address}) still unreachable.", brain.ServiceEndPoint.Address);
            }
        }
    }

    public readonly record struct RegisterServerRequest(string Name, string BaseUrl);
    public readonly record struct UnregisterServerRequest(string Name);
}
