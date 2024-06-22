using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Rest;

/// <summary>
/// Hosted service responsible for registering the integration server with the NEEO Brain.
/// </summary>
internal sealed class SdkRegistration(
    IBrain brain,
    IApiClient client,
    ISdkEnvironment environment,
    ILogger<SdkRegistration> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await client.PostAsync(UrlPaths.RegisterServer, new { Name = environment.SdkAdapterName, BaseUrl = environment.HostAddress }, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Server {name} registered on {brain} ({brainAddress}).", environment.SdkAdapterName, brain.HostName, brain.ServiceEndPoint.Address);
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed to unregister with brain - {content}.", e.Message);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await client.PostAsync(UrlPaths.UnregisterServer, new { name = environment.SdkAdapterName }, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Server unregistered from {brain}.", brain.HostName);
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed to unregister with brain - {content}.", e.Message);
        }
    }
}
