namespace Remote.Neeo.Devices.Descriptors
{
    public abstract record Descriptor
    {
        protected Descriptor(string name, string? label) => (this.Name, this.Label) = (name, label);

        /// <summary>
        /// The name of the item being described.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// An optional display label to use in place of the name on the NEEO remote.
        /// </summary>
        public string? Label { get; }
    }
}
