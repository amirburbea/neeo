using System.Text.Json;
using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex;

partial class PlexServer
{
    [JsonConverter(typeof(TextJsonConverter<LibraryDirectoryType>))]
    public enum LibraryDirectoryType
    {
        [Text("movie")]
        Movie = 0,

        [Text("show")]
        Show,

        [Text("artist")]
        Artist,

        [Text("photo")]
        Photo,

        [Text("album")]
        Album,

        [Text("season")]
        Season,
    }

    [JsonConverter(typeof(TextJsonConverter<ServerNotificationType>))]
    internal enum ServerNotificationType
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

    internal record struct ClientsMediaContainer(
        [property: JsonPropertyName("Server")] ClientServer[]? Clients
    );

    internal record struct ClientServer(
        string Name,
        string Host,
        string Address,
        int Port,
        string MachineIdentifier,
        string Version,
        string Protocol,
        string Product,
        string DeviceClass,
        int ProtocolVersion,
        PlayerCapabilities ProtocolCapabilities
    );

    internal record struct PlayStateNotification(
        string ClientIdentifier,
        string Guid,
        string Key,
        [property: JsonPropertyName("playQueueID")] int PlayQueueId,
        [property: JsonPropertyName("playQueueItemID")] int PlayQueueItemId,
        int RatingKey,
        string SessionKey,
        PlayState State,
        string Url,
        int ViewOffset
    );

    internal record struct Response<TContainer>(
        [property: JsonPropertyName("MediaContainer")] TContainer MediaContainer
    ) where TContainer : struct;

    internal record struct ServerMessage(
        [property: JsonPropertyName("NotificationContainer")] ServerNotificationContainer? Container
    );

    internal record struct ServerNotificationContainer(
        int Size,
        [property: JsonPropertyName("Type")] ServerNotificationType Type,
        [property: JsonPropertyName("PlaySessionStateNotification")] JsonElement? Notifications
    );

    internal record struct MediaItemDetailContainer(
        [property: JsonPropertyName("Metadata")] MediaItemMetadata[]? Metadata
    );

    private enum MediaCategory
    {
        [Text("photo")]
        Photo = 0,
        [Text("video")]
        Video = 1,
        [Text("music")]
        Music = 2
    }

    internal record struct MediaItemMetadata
    (
        int RatingKey,
        MediaType Type,
        string Title,
        string? Summary = null,
        [property: JsonPropertyName("thumb")] string? Thumbnail = null,
        int? ViewOffset = null,
        [property: JsonPropertyName("Player")] MediaPlayer? Player = null,
        int? Duration = null
    );

    internal record struct MediaPlayer(string MachineIdentifier, PlayState State);

    internal record struct LibraryDirectory(
        string Key,
        string Title,
        bool Secondary = false,
        int? RatingKey = null,
        [property: JsonPropertyName("thumb")] string? Thumbnail = null,
        LibraryDirectoryType? Type = null,
        int? Size = null
    );

    internal record struct LibraryMediaContainer(
        [property: JsonPropertyName("title1")] string Title,
        [property: JsonPropertyName("Directory")] LibraryDirectory[]? Directories,
        [property: JsonPropertyName("Metadata")] MediaItemMetadata[]? Metadata,
        int TotalSize,
        [property: JsonPropertyName("title2")] string? Subtitle = null,
        [property: JsonPropertyName("thumb")] string? Thumbnail = null,
        [property: JsonPropertyName("librarySectionID")] int? LibrarySectionId = null,
        string? ViewGroup = null,
        string? Art = null
    );
}
