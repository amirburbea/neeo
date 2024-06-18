using Microsoft.Extensions.DependencyInjection;

namespace Neeo.Sdk.Utilities;

/// <summary>
/// When implemented in libraries loaded in the integration server <c>Neeo.Sdk.Server</c>,
/// will be invoked before startup to allow for registering dependencies.
/// </summary>
public interface IServiceConfiguration
{
    /// <summary>
    /// Configure services in the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    void ConfigureServices(IServiceCollection services);
}
