using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

internal partial class PlexServer
{
    private record struct Activity(
        [property: JsonPropertyName("cancellable")] bool Cancelable,
        int Progress,
        string Subtitle,
        string Title,
        string Type,
        [property: JsonPropertyName("userID")] int UserId,
        string Uuid
    );

    private record struct ActivityNotification(
        [property: JsonPropertyName("Activity")] Activity Activity,
        string Event,
        string Uuid
    );

    private record struct NotificationContainer(
        [property: JsonPropertyName("ActivityNotification")] ActivityNotification[]? ActivityNotifications,
        [property: JsonPropertyName("PlaySessionStateNotification")] PlaySessionStateNotification[]? PlaySessionStateNotifications,
        [property: JsonPropertyName("ReachabilityNotification")] ReachabilityNotification[]? ReachabilityNotifications,
        int Size,
        [property: JsonPropertyName("StatusNotification")] StatusNotification[]? StatusNotifications,
        [property: JsonPropertyName("TimelineEntry")] TimelineEntry[]? TimelineEntries,
        ServerNotificationType Type
    )
    {
        [JsonExtensionData] public Dictionary<string, JsonElement> AdditionalData { get; set; } = [];
    }

    private record struct ServerMessage(
        [property: JsonPropertyName("NotificationContainer")] NotificationContainer Notifications,
        int Unused
    );

    private record struct StatusNotification(
        string Description,
        string NotificationName,
        string Title
    );

    private record struct ReachabilityNotification(
        bool Reachability
    );

    private record struct TimelineEntry(
        string Identifier,
        [property: JsonPropertyName("itemID")] int ItemId,
        string MetadataState,
        [property: JsonPropertyName("sectionID")] int SectionId,
        int State,
        int Type,
        long UpdatedAt,
        string? Title
    );

    private record struct PlaySessionStateNotification(
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

    [JsonConverter(typeof(TextJsonConverter<ServerNotificationType>))]
    private enum ServerNotificationType
    {
        Unknown = 0,

        [Text("playing")]
        Playing,

        [Text("reachability")]
        Reachability,

        [Text("transcode.end")]
        TranscodeEnd,

        [Text("preference")]
        Preference,

        [Text("update.statechange")]
        StateChange,

        [Text("activity")]
        Activity,

        [Text("backgroundProcessingQueue")]
        BackgroundProcessingQueue,

        [Text("transcodeSession.update")]
        TranscodeSessionUpdate,

        [Text("transcodeSession.end")]
        TranscodeSessionEnd,
    }
}
