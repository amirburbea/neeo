using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.PlexApi;

partial class PlexServer
{
    [JsonConverter(typeof(TextJsonConverter<ServerNotificationType>))]
    private enum ServerNotificationType
    {
        Unknown = 0,

        [Text("playing")]
        Playing,

        [Text("reachability")]
        Reachability,

        [Text("preference")]
        Preference,

        [Text("update.statechange")]
        StateChange,

        [Text("activity")]
        Activity,

        [Text("timeline")]
        Timeline,

        [Text("backgroundProcessingQueue")]
        BackgroundProcessingQueue,

        [Text("transcodeSession.start")]
        TranscodeSessionStart,

        [Text("transcodeSession.update")]
        TranscodeSessionUpdate,

        [Text("transcodeSession.end")]
        TranscodeSessionEnd,
    }

    private record struct ClientsMediaContainer(
        [property: JsonPropertyName("Server")] ClientServer[]? Clients
    );

    private record struct ClientServer(
        string Name,
        string Host,
        string Address,
        int Port,
        string MachineIdentifier,
        string Version,
        string Protocol,
        string Product,
        string DeviceClass,
        double ProtocolVersion,
        PlayerCapabilities ProtocolCapabilities
    );

    private record struct PlaySessionStateNotification(
        string ClientIdentifier,
        string Guid,
        string Key,
        [property: JsonPropertyName("playQueueID")] int PlayQueueId,
        [property: JsonPropertyName("playQueueItemID")] int PlayQueueItemId,
        string RatingKey,
        string SessionKey,
        PlayState State,
        string Url,
        int ViewOffset
    );

    private record struct Response<TContainer>(
        [property: JsonPropertyName("MediaContainer")]
        TContainer MediaContainer
    ) where TContainer : struct;

    private record struct ServerMessage(
        [property: JsonPropertyName("NotificationContainer")] ServerNotificationContainer? Notifications
    );

    private record struct ServerNotificationContainer(
        int Size,
        [property: JsonPropertyName("Type")] ServerNotificationType Type,
        [property: JsonPropertyName("PlaySessionStateNotification")] PlaySessionStateNotification[]? PlaySessionStateNotifications
    );
}
