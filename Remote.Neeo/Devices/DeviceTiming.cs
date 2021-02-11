using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices
{
    /// <summary>
    /// A structure for specifying the delays NEEO should use when interacting with a device.
    /// </summary>
    public readonly struct DeviceTiming
    {
        public static readonly DeviceTiming Empty = new();

        /// <summary>
        /// Initialize a new <see cref="DeviceTiming"/> instance.
        /// </summary>
        /// <param name="powerOnDelay">
        /// Optional: The number of milliseconds NEEO should wait after powering on the device
        /// before sending it another command.
        /// </param>
        /// <param name="sourceSwitchDelay">
        /// Optional: The number of milliseconds NEEO should wait after switching input on the device
        /// before sending it another command.
        /// </param>
        /// <param name="shutdownDelay">
        /// Optional: The number of milliseconds NEEO should wait after shutting down the device
        /// before sending it another command.
        /// </param>
        public DeviceTiming(int? powerOnDelay = default, int? shutdownDelay = default, int? sourceSwitchDelay = default)
        {
            Validator.ValidateDelay(this.PowerOnDelay = powerOnDelay, nameof(powerOnDelay));
            Validator.ValidateDelay(this.ShutdownDelay = shutdownDelay, nameof(shutdownDelay));
            Validator.ValidateDelay(this.SourceSwitchDelay = sourceSwitchDelay, nameof(sourceSwitchDelay));
        }

        /// <summary>
        /// Specifies the number of milliseconds NEEO should wait after powering on the device
        /// before sending it another command.
        /// </summary>
        [JsonPropertyName("standbyCommandDelay")]
        public int? PowerOnDelay { get; }

        /// <summary>
        /// Specifies the number of milliseconds NEEO should wait after shutting down the device
        /// before sending it another command.
        /// </summary>
        public int? ShutdownDelay { get; }

        /// <summary>
        /// Specifies the number of milliseconds NEEO should wait after switching input on the device
        /// before sending it another command.
        /// </summary>
        public int? SourceSwitchDelay { get; }
    }
}
