using System;
using System.Threading;
using System.Threading.Tasks;
using Neeo.Sdk;
using Neeo.Sdk.Utilities;

namespace Neeo.Sdk.Server;

internal interface ISdkServerStarter
{
    Task<ISdkEnvironment> StartServerAsync(
        Brain brain,
        Type[] providerTypes,
        string? name,
        IServiceConfiguration[] serviceConfigurations,
        CancellationToken cancellationToken
    );
}

internal sealed class SdkServerStarter : ISdkServerStarter
{
    public Task<ISdkEnvironment> StartServerAsync(Brain brain, Type[] providerTypes, string? name, IServiceConfiguration[] serviceConfigurations, CancellationToken cancellationToken) => brain.StartServerAsync(
        providerTypes,
        name: name,
        serviceConfigurations: serviceConfigurations,
        cancellationToken: cancellationToken
    );
}
