using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Neeo.Drivers.Plex.ServerNotifications;

internal record struct NotificationContainer(
    [property: JsonPropertyName(nameof(ActivityNotification))] ActivityNotification[]? ActivityNotifications,
    [property: JsonPropertyName(nameof(PlaySessionStateNotification))] PlaySessionStateNotification[]? PlaySessionStateNotifications,
    [property: JsonPropertyName(nameof(ReachabilityNotification))] ReachabilityNotification[]? ReachabilityNotifications,
    int Size,
    [property: JsonPropertyName(nameof(StatusNotification))] StatusNotification[]? StatusNotifications,
    [property: JsonPropertyName(nameof(TimelineEntry))] TimelineEntry[]? TimelineEntries,
    ServerNotificationType Type
)
{
    [JsonExtensionData] public Dictionary<string, JsonElement> AdditionalData { get; set; } = [];
}
