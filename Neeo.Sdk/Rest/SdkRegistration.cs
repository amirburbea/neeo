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
    private readonly Brain _brain;
    private readonly IApiClient _client;
    private readonly ISdkEnvironment _environment;
    private readonly ILogger<SdkRegistration> _logger;

    public SdkRegistration(Brain brain, IApiClient client, ISdkEnvironment environment, ILogger<SdkRegistration> logger)
    {
        (this._brain, this._client, this._environment, this._logger) = (brain, client, environment, logger);
    }

    private string BaseUrl => this._environment.HostAddress;

    private string Name => this._environment.SdkAdapterName;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (!await this.PostAsync(UrlPaths.RegisterServer, new { this.Name, this.BaseUrl }, cancellationToken).ConfigureAwait(false))
            {
                throw new ApplicationException("Registration rejected.");
            }
            this._logger.LogInformation("Server {name} registered on {brain} ({brainAddress}).", this.Name, this._brain.HostName, this._brain.IPAddress);
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
            if (await this.PostAsync(UrlPaths.UnregisterServer, new { this.Name }, cancellationToken).ConfigureAwait(false))
            {
                this._logger.LogInformation("Server unregistered from {brain}.", this._brain.HostName);
            }
        }
        catch (Exception e)
        {
            this._logger.LogWarning("Failed to unregister with brain - {content}.", e.Message);
        }
    }

    private Task<bool> PostAsync<TBody>(string path, TBody body, CancellationToken cancellationToken) => this._client.PostAsync(
        path,
        body,
        static (SuccessResponse response) => response.Success,
        cancellationToken
    );
}