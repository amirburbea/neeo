namespace Remote.Neeo.Devices.Descriptors
{
    public sealed record TextLabelDescriptor : Descriptor
    {
        public TextLabelDescriptor(string name, string? label = default, bool? isLabelVisible = default)
            : base(name, label)
        {
            this.IsLabelVisible = isLabelVisible;
        }

        public bool? IsLabelVisible { get; }
    }
}
