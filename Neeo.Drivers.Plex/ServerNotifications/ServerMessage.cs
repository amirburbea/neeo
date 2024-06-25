using System.Text.Json.Serialization;

namespace Neeo.Drivers.Plex.ServerNotifications;

internal record struct ServerMessage(
    [property: JsonPropertyName(nameof(NotificationContainer))] NotificationContainer Notifications
);
