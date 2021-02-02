namespace Remote.Neeo.Devices
{
    public record ButtonDescriptor
    {
        public ButtonDescriptor(string name, string? label = default) => (this.Name, this.Label) = (name, label);

        public string Name { get; }

        public string? Label { get; }
    }
}
