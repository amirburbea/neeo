using System.Text.Json.Serialization;

namespace Neeo.Drivers.Plex.ServerNotifications;

internal record struct ActivityNotification(
    [property: JsonPropertyName(nameof(Activity))] Activity Activity,
    string Event,
    string Uuid
);
