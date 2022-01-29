using System.Net;

namespace Neeo.Sdk;

public interface ISdkEnvironment
{
    string AdapterName { get; }

    IPEndPoint BrainEndPoint { get; }

    string BrainHostName { get; }

    IPEndPoint HostEndPoint { get; }

    void Deconstruct(out string adapterName, out IPEndPoint brainEndPoint, out string brainHostName, out IPEndPoint hostEndPoint);
}

internal record struct SdkEnvironment(
    string AdapterName,
    IPEndPoint BrainEndPoint, 
    string BrainHostName, 
    IPEndPoint HostEndPoint
) : ISdkEnvironment;