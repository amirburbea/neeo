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
    /// The model representing the NEEO Brain.
    /// </summary>
    Brain Brain { get; }

    /// <summary>
    /// The host server address.
    /// </summary>
    string HostAddress { get; }
}

internal sealed class SdkEnvironment : ISdkEnvironment
{
    private readonly SdkConfiguration _sdkConfiguration;
    private readonly IServerAddressesFeature _serverAddressesFeature;

    public SdkEnvironment(SdkConfiguration sdkConfiguration, IServer server)
    {
        this._sdkConfiguration = sdkConfiguration;
        this._serverAddressesFeature = server.Features.Get<IServerAddressesFeature>()!;
    }

    public string AdapterName => this._sdkConfiguration.Name;

    public Brain Brain => this._sdkConfiguration.Brain;

    public string HostAddress => this._serverAddressesFeature.Addresses.Single();
}