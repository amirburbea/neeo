namespace Remote.Neeo.Devices.Descriptors
{
    public sealed record ButtonDescriptor : DescriptorBase
    {
        public ButtonDescriptor(string name, string? label = default)
            : base(name, label)
        {
        }

        public static implicit operator ButtonDescriptor(string name) => new(name);
    }
}
