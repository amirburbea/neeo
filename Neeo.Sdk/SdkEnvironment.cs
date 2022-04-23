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
    /// The host server address.
    /// </summary>
    string HostAddress { get; }

    /// <summary>
    /// The encoded SDK adapter name as registered on the NEEO Brain.
    /// </summary>
    string SdkAdapterName { get; }

    /// <summary>
    /// Deconstructs this instance.
    /// </summary>
    void Deconstruct(out string sdkAdapterName, out string hostAddress) => (sdkAdapterName, hostAddress) = (this.SdkAdapterName, this.HostAddress);
}

internal sealed class SdkEnvironment : ISdkEnvironment
{
    private readonly IServerAddressesFeature _serverAddressesFeature;

    public SdkEnvironment(SdkAdapterName sdkAdapterName, IServer server)
    {
        (this.SdkAdapterName, this._serverAddressesFeature) = ((string)sdkAdapterName, server.Features.Get<IServerAddressesFeature>()!);
    }

    public string HostAddress => this._serverAddressesFeature.Addresses.Single();

    public string SdkAdapterName { get; }
}