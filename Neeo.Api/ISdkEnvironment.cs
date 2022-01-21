namespace Neeo.Api;

/// <summary>
/// Interface for a class which contains information about the current SDK Environment.
/// </summary>
public interface ISdkEnvironment
{
    /// <summary>
    /// Gets the NEEO Brain.
    /// </summary>
    Brain Brain { get;  }
    /// <summary>
    /// Gets the name of the SDK adapter.
    /// </summary>
    string SdkAdapterName { get;  }

    void Deconstruct(out string sdkAdapterName, out Brain brain);
}