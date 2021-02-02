using System.Text.Json.Serialization;

namespace Remote.Neeo.Devices
{
    public record DelaysSpecifier
    {
        public static readonly DelaysSpecifier Empty = new();

        public DelaysSpecifier(int? powerOnDelay = default, int? shutdownDelay = default, int? sourceSwitchDelay = default)
        {
            Validator.ValidateDelay(this.PowerOnDelay = powerOnDelay, nameof(powerOnDelay));
            Validator.ValidateDelay(this.ShutdownDelay = shutdownDelay, nameof(shutdownDelay));
            Validator.ValidateDelay(this.SourceSwitchDelay = sourceSwitchDelay, nameof(sourceSwitchDelay));
        }

        [JsonPropertyName("standbyCommandDelay")]
        public int? PowerOnDelay { get; }

        public int? ShutdownDelay { get; }

        public int? SourceSwitchDelay { get; }
    }
}
