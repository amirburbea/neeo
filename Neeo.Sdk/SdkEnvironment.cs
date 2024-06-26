﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    /// Instructs the Integration Server to stop processing requests and shut down, gracefully if possible.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests to abort the shutdown.</param>
    /// <returns><see cref="Task"/> representing the asynchronous operation.</returns>
    Task StopAsync(CancellationToken cancellationToken = default);
}

internal sealed class SdkEnvironment(
    SdkAdapterName sdkAdapterName,
    IServer server
) : ISdkEnvironment
{
    public string HostAddress => server.Features.Get<IServerAddressesFeature>()!.Addresses.Single();

    public string SdkAdapterName { get; } = (string)sdkAdapterName;

    public Task StopAsync(CancellationToken cancellationToken) => server.StopAsync(cancellationToken);
}
