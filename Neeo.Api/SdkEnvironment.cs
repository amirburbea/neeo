using System.Net;

namespace Neeo.Api;

public sealed record class SdkEnvironment(
    string SdkAdapterName,
    IPEndPoint HostEndPoint,
    IPEndPoint BrainEndPoint,
    string BrainHostName
) ;