using System.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Neeo.Sdk;

/// <summary>
/// A structure containing details about the currently running SDK environment.
/// </summary>
public interface ISdkEnvironment
{
    /// <summary>
    /// The encoded SDK adapter name as registered on the NEEO Brain.
    /// </summary>
    string AdapterName { get; }

    /// <summary>
    /// The host server address.
    /// </summary>
    string HostAddress { get; }
}

internal sealed class SdkEnvironment : ISdkEnvironment
{
    private readonly IServerAddressesFeature _serverAddressesFeature;

    public SdkEnvironment(SdkAdapterName adapterName, IServer server)
    {
        (this.AdapterName, this._serverAddressesFeature) = ((string)adapterName, server.Features.Get<IServerAddressesFeature>()!);
    }

    public string AdapterName { get; }

    public string HostAddress => this._serverAddressesFeature.Addresses.Single();
}