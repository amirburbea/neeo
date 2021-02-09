namespace Remote.Neeo.Devices.Descriptors
{
    public abstract record DescriptorBase
    {
        protected DescriptorBase(string name, string? label) => (this.Name, this.Label) = (name, label);

        public string Name { get; }

        public string? Label { get; }
    }
}
