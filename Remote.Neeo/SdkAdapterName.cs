namespace Remote.Neeo
{
    internal sealed record SdkAdapterName(string Name)
    {
        public static implicit operator SdkAdapterName(string name) => new(name);
    }
}