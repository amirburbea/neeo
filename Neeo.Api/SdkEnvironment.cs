using System.Net;

namespace Neeo.Api;

public sealed record class SdkEnvironment(string AdapterName, IPEndPoint HostEndPoint, IPEndPoint BrainEndPoint, string BrainHostName);