using System.Net;

namespace Neeo.Api;

/// <summary>
/// A class which contains information about the current SDK Environment.
/// </summary>
/// <param name="SdkAdapterName">The name of the SDK adapter.</param>
/// <param name="Brain">The NEEO Brain.</param>
public sealed record class SdkEnvironment(string SdkAdapterName, Brain Brain);