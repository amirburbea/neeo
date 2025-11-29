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
            RegisterServerRequest request = new(environment.SdkAdapterName, environment.HostAddress);
            await client.PostAsync(BrainUrlPaths.RegisterServer, request, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Server {Name} registered on {Brain} ({Address}).", environment.SdkAdapterName, brain.HostName, brain.ServiceEndPoint.Address);
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed to register with brain - {Content}.", e.Message);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            UnregisterServerRequest request = new(environment.SdkAdapterName);
            await client.PostAsync(BrainUrlPaths.UnregisterServer, request, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Server unregistered from {brain}.", brain.HostName);
        }
        catch (Exception e)
        {
            logger.LogWarning("Failed to unregister with brain - {content}.", e.Message);
        }
    }

    public readonly record struct RegisterServerRequest(string Name, string BaseUrl);
    public readonly record struct UnregisterServerRequest(string Name);
}
