using System.Net;

namespace Neeo.Api;

/// <summary>
/// Interface for a class which contains information about the current SDK Environment.
/// </summary>
public interface ISdkEnvironment
{
    IPEndPoint BrainEndPoint { get; }

    string BrainHostName { get; }

    IPEndPoint HostEndPoint { get; }

    /// <summary>
    /// Gets the name of the SDK adapter.
    /// </summary>
    string SdkAdapterName { get; }
}