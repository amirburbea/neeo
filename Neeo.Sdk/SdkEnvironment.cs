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

public sealed class SdkEnvironment : ISdkEnvironment
{
    internal SdkEnvironment(string adapterName, IPEndPoint brainEndPoint, string brainHostName, IPEndPoint hostEndPoint)
    {
        (this.AdapterName, this.BrainEndPoint, this.BrainHostName, this.HostEndPoint) = (adapterName, brainEndPoint, brainHostName, hostEndPoint);
    }

    public string AdapterName { get; }

    public IPEndPoint BrainEndPoint { get; }

    public string BrainHostName { get; }

    public IPEndPoint HostEndPoint { get; }

    public void Deconstruct(out string adapterName, out IPEndPoint brainEndPoint, out string brainHostName, out IPEndPoint hostEndPoint)
    {
        (adapterName, brainEndPoint, brainHostName, hostEndPoint) = (this.AdapterName, this.BrainEndPoint, this.BrainHostName, this.HostEndPoint);
    }
}