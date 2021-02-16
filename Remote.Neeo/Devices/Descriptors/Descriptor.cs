namespace Remote.Neeo.Devices.Descriptors
{
    /// <summary>
    /// Provides a spec or descriptor for NEEO remote items.
    /// </summary>
    public abstract record Descriptor
    {
        /// <summary>
        /// Creates a new instance of the <see cref="Descriptor"/> record.
        /// </summary>
        /// <param name="name">The name of the item being described.</param>
        /// <param name="label">An optional display label to use in place of the name on the NEEO remote.</param>
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
