namespace Neeo.Sdk;

internal sealed record class SdkAdapterName(string Name)
{
    public static explicit operator string(SdkAdapterName adapterName) => adapterName.Name;

    public static explicit operator SdkAdapterName(string adapterName) => new(adapterName);
}