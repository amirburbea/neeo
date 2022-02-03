using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Neeo.Sdk.Rest;

internal sealed class SdkRegistration : IHostedService
{
    private readonly IApiClient _client;
    private readonly ISdkEnvironment _environment;
    private readonly ILogger<SdkRegistration> _logger;
    private readonly IServer _server;

    public SdkRegistration(
        IApiClient client,
        IServer server,
        ISdkEnvironment environment,
        ILogger<SdkRegistration> logger
    )
    {
        (this._client, this._environment, this._logger) = (client, environment, logger);
        this._server = server;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        (string adapterName, Brain brain, string hostAddress) = (this._environment.AdapterName, this._environment.Brain, this._environment.HostAddress);
        for (int i = 0; i <= Constants.MaxRetries; i++)
        {
            try
            {
                if (!await this._client.PostAsync(UrlPaths.RegisterServer, new { Name = adapterName, BaseUrl = hostAddress }, (SuccessResponse response) => response.Success, cancellationToken).ConfigureAwait(false))
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
            if (await this._client.PostAsync(UrlPaths.UnregisterServer, new { Name = this._environment.AdapterName }, (SuccessResponse response) => response.Success, cancellationToken).ConfigureAwait(false))
            {
                this._logger.LogInformation("Server unregistered from {brain}.", this._environment.Brain.HostName);
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