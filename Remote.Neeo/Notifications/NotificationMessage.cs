using System;
using System.Text.Json.Serialization;

namespace Remote.Neeo.Notifications
{
    public readonly struct NotificationMessage : IEquatable<NotificationMessage>
    {
        public const string DeviceSensorUpdateType = "DEVICE_SENSOR_UPDATE";

        [JsonConstructor]
        public NotificationMessage(string type, object data)
        {
            this.Type = type ?? throw new ArgumentNullException(nameof(type));
            this.Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public object Data { get; }

        public string Type { get; }

        public void Deconstruct(out string type, out object data) => (type, data) = (this.Type, this.Data);

        public bool Equals(NotificationMessage other) => this.Type == other.Type && this.Data.Equals(other.Data);

        public override bool Equals(object? obj) => obj is NotificationMessage message && this.Equals(message);

        public override int GetHashCode() => HashCode.Combine(this.Type, this.Data);

        public bool IsEmpty() => this.Type == null;
    }
}