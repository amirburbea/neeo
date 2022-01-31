using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Neeo.Sdk.Rest;

internal sealed class ServerRegistration : IHostedService
{
    private readonly IApiClient _client;
    private readonly ISdkEnvironment _environment;
    private readonly ILogger<ServerRegistration> _logger;

    public ServerRegistration(
        IApiClient client,
        ISdkEnvironment environment,
        ILogger<ServerRegistration> logger
    ) => (this._client, this._environment, this._logger) = (client, environment, logger);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        (string adapterName, IPEndPoint brainEndPoint, string brainHostName, IPEndPoint hostEndPoint) = this._environment;
        object body = new { Name = adapterName, BaseUrl = $"http://{hostEndPoint}" };
        for (int i = 0; i <= Constants.MaxRetries; i++)
        {
            try
            {
                if (!await this._client.PostAsync(UrlPaths.RegisterServer, body, static (SuccessResponse response) => response.Success, cancellationToken).ConfigureAwait(false))
                {
                    throw new ApplicationException("Failed to register on the brain - registration rejected.");
                }
                this._logger.LogInformation("Server {adapterName} registered on {brain} ({brainIP}).", adapterName, brainHostName, brainEndPoint.Address);
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
            (string adapterName, _, string brainHostName, _) = this._environment;
            if (await this._client.PostAsync(UrlPaths.UnregisterServer, new { Name = adapterName }, static (SuccessResponse response) => response.Success, cancellationToken).ConfigureAwait(false))
            {
                this._logger.LogInformation("Server unregistered from {brain}.", brainHostName);
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