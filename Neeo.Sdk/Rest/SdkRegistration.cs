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

    public SdkRegistration(IApiClient client, ISdkEnvironment environment, ILogger<SdkRegistration> logger)
    {
        (this._client, this._environment, this._logger) = (client, environment, logger);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (int i = 0; i <= Constants.MaxRetries; i++)
        {
            try
            {
                (Brain brain, string adapterName, string hostAddress) = this._environment;
                if (!await this.PostAsync(UrlPaths.RegisterServer, new { Name = adapterName, BaseUrl = hostAddress }, cancellationToken).ConfigureAwait(false))
                {
                    throw new ApplicationException("Failed to register on the brain - registration rejected.");
                }
                this._logger.LogInformation("Server {adapterName} registered on {brain} ({brainAddress}).", adapterName, brain.HostName, brain.IPAddress);
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
            (Brain brain, string adapterName) = this._environment;
            if (await this.PostAsync(UrlPaths.UnregisterServer, new { Name = adapterName }, cancellationToken).ConfigureAwait(false))
            {
                this._logger.LogInformation("Server unregistered from {brain}.", brain.HostName);
            }
        }
        catch (Exception e)
        {
            this._logger.LogWarning("Failed to unregister with brain - {content}.", e.Message);
        }
    }

    /// <summary>
    /// Makes a POST request to the NEEO Brain with an expected return type of <see cref="SuccessResponse"/> and returns the success value.
    /// </summary>
    private Task<bool> PostAsync<TBody>(string path, TBody body, CancellationToken cancellationToken) => this._client.PostAsync(
        path,
        body,
        (SuccessResponse response) => response.Success,
        cancellationToken
    );

    private static class Constants
    {
        public const int MaxRetries = 8;
    }
}