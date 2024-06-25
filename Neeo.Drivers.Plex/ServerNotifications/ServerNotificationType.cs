using System.Text.Json.Serialization;
using Neeo.Sdk.Utilities;

namespace Neeo.Drivers.Plex.ServerNotifications;

[JsonConverter(typeof(TextJsonConverter<ServerNotificationType>))]
internal enum ServerNotificationType
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