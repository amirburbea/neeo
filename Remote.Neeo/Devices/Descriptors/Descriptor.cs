namespace Remote.Neeo.Devices.Descriptors
{
    public abstract record Descriptor
    {
        protected Descriptor(string name, string? label) => (this.Name, this.Label) = (name, label);

        public string Name { get; }

        public string? Label { get; }
    }
}
