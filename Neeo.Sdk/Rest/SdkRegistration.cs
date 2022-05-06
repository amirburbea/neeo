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
    private readonly IBrainInfo _brain;
    private readonly IApiClient _client;
    private readonly ISdkEnvironment _environment;
    private readonly ILogger<SdkRegistration> _logger;

    public SdkRegistration(IBrainInfo brain, IApiClient client, ISdkEnvironment environment, ILogger<SdkRegistration> logger)
    {
        (this._brain, this._client, this._environment, this._logger) = (brain, client, environment, logger);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            (string name, string baseUrl) = this._environment;
            if (!await this.PostAsync(UrlPaths.RegisterServer, new { name, baseUrl }, cancellationToken).ConfigureAwait(false))
            {
                throw new ApplicationException("Registration rejected.");
            }
            this._logger.LogInformation("Server {name} registered on {brain} ({brainAddress}).", name, this._brain.HostName, this._brain.ServiceEndPoint.Address);
        }
        catch (Exception e)
        {
            this._logger.LogWarning("Failed to unregister with brain - {content}.", e.Message);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (await this.PostAsync(UrlPaths.UnregisterServer, new { name = this._environment.SdkAdapterName }, cancellationToken).ConfigureAwait(false))
            {
                this._logger.LogInformation("Server unregistered from {brain}.", this._brain.HostName);
            }
        }
        catch (Exception e)
        {
            this._logger.LogWarning("Failed to unregister with brain - {content}.", e.Message);
        }
    }

    private Task<bool> PostAsync<TBody>(string path, TBody body, CancellationToken cancellationToken)
        where TBody : notnull
    {
        return this._client.PostAsync(
            path,
            body,
            static (SuccessResponse response) => response.Success,
            cancellationToken
        );
    }
}