namespace Neeo.Sdk;

/// <summary>
/// Wrapper for the sdk adapter name for use in dependency injection.
/// </summary>
/// <param name="name">The sdk adapter name.</param>
internal sealed class SdkAdapterName(string name)
{
    public string Name { get; } = name;

    public static explicit operator SdkAdapterName(string adapterName) => new(adapterName);

    public static explicit operator string(SdkAdapterName adapterName) => adapterName.Name;

    public override string ToString() => this.Name;
}
