namespace Neeo.Api;

/// <summary>
/// A class which contains information about the current SDK Environment.
/// </summary>
/// <param name="SdkAdapterName">The name of the SDK adapter.</param>
internal record class SdkEnvironment(string SdkAdapterName);