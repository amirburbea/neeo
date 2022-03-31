using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Neeo.Sdk.Rest;

/// <summary>
/// Hosted service responsible for registering the integration server with the NEEO Brain.
/// </summary>
internal sealed class SdkRegistration : IHostedService
{
    private readonly IApiClient _client;
    private readonly ISdkEnvironment _environment;
    private readonly ILogger<SdkRegistration> _logger;
    private readonly Brain _brain;

    public SdkRegistration(Brain brain, IApiClient client, ISdkEnvironment environment, ILogger<SdkRegistration> logger)
    {
        (this._brain, this._client, this._environment, this._logger) = (brain, client, environment, logger);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i <= Constants.MaxRetries; i++)
        {
            try
            {

                if (!await this._client.PostAsync(UrlPaths.RegisterServer, new { Name = this._environment.AdapterName, BaseUrl = this._environment.HostAddress }, static (SuccessResponse response) => response.Success, cancellationToken).ConfigureAwait(false))
                {
                    throw new ApplicationException("Failed to register on the brain - registration rejected.");
                }
                this._logger.LogInformation("Server {adapterName} registered on {brain} ({brainAddress}).", this._environment.AdapterName, this._brain.HostName, this._brain.IPAddress);
                break;
            }
            catch (Exception) when (i != Constants.MaxRetries)
            {
                this._logger.LogWarning("Failed to register with brain (on attempt #{attempt}). Retrying...", i + 1);
                continue;
            }
            catch (Exception e)
            {
                this._logger.LogError(e, "Failed to register on the brain - giving up.");
                throw;
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (await this._client.PostAsync(UrlPaths.UnregisterServer, new { Name = this._environment.AdapterName }, static (SuccessResponse response) => response.Success, cancellationToken).ConfigureAwait(false))
            {
                this._logger.LogInformation("Server unregistered from {brain}.", this._brain.HostName);
            }
        }
        catch (Exception e)
        {
            this._logger.LogWarning("Failed to unregister with brain - {content}.", e.Message);
        }
    }

    private static class Constants
    {
        public const int MaxRetries = 8;
    }
}