namespace Remote.Neeo
{
    /// <summary>
    /// A class which contains information about the current SDK Environment.
    /// </summary>
    internal sealed class SdkEnvironment
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SdkEnvironment"/> class.
        /// </summary>
        /// <param name="name">The name of the SDK adapter.</param>
        public SdkEnvironment(string name) => this.Name = name;

        /// <summary>
        /// Gets the name of the SDK Adapter.
        /// </summary>
        public string Name { get; }
    }
}