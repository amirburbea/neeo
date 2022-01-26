using System.Net;

namespace Neeo.Sdk;

public sealed record class SdkEnvironment(string AdapterName, IPEndPoint HostEndPoint, IPEndPoint BrainEndPoint, string BrainHostName);