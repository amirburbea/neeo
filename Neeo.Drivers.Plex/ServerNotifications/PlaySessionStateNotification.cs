using System.Text.Json.Serialization;

namespace Neeo.Drivers.Plex.ServerNotifications;

internal record struct PlaySessionStateNotification(
    string ClientIdentifier,
    string Guid,
    string Key,
    [property: JsonPropertyName("playQueueID")] int PlayQueueId,
    [property: JsonPropertyName("playQueueItemID")] int PlayQueueItemId,
    string RatingKey,
    string SessionKey,
    string State,
    string Url,
    int ViewOffset
);
