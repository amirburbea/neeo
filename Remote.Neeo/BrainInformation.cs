using System.Text.Json.Serialization;

namespace Remote.Neeo
{
    /// <summary>
    /// A struct containing NEEO Brain system information.
    /// </summary>
    public readonly struct BrainInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BrainInformation"/> struct.
        /// </summary>
        [JsonConstructor]
        public BrainInformation(string ip, string wirelessIP, string wirelessRegion, string firmwareVersion, string version, string hardwareRegion, int hardwareRevision, string user)
        {
            this.IP = ip;
            this.WirelessIP = wirelessIP;
            this.FirmwareVersion = firmwareVersion;
            this.Version = version;
            this.User = user;
            this.WirelessRegion = wirelessRegion;
            this.HardwareRevision = hardwareRevision;
            this.HardwareRegion = hardwareRegion;
        }

        /// <summary>
        /// Gets the firmware version.
        /// </summary>
        public string FirmwareVersion { get; }

        /// <summary>
        /// Gets the IP address of the Brain.
        /// </summary>
        [JsonPropertyName("ip")]
        public string IP { get; }

        /// <summary>
        /// Gets the version of the software running on the Brain.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Gets the wireless IP address of the Brain. This may or may not be the same <see cref="BrainInformation.IP" />.
        /// </summary>
        [JsonPropertyName("wlanip")]
        public string WirelessIP { get; }

        /// <summary>
        /// Gets the user registered to the Brain.
        /// </summary>
        public string User { get; }

        /// <summary>
        /// Gets the hardware region code of the Brain.
        /// </summary>
        [JsonPropertyName("hardwareregion")]
        public string HardwareRegion { get; }

        /// <summary>
        /// Gets the hardware revision of the Brain.
        /// </summary>
        public int HardwareRevision { get; }

        /// <summary>
        /// Gets the wireless region code of the Brain.
        /// </summary>
        [JsonPropertyName("wlanregion")]
        public string WirelessRegion { get; }
    }
}