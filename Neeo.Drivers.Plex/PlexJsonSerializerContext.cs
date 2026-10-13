using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neeo.Drivers.Plex;

/// <summary>
/// Source-generated JSON metadata for Plex REST and WebSocket wire types.
/// Combined with a reflection fallback in <see cref="PlexJsonSerializerOptions"/>.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
)]
[JsonSerializable(typeof(PlayerData))]
[JsonSerializable(typeof(MediaType))]
[JsonSerializable(typeof(PlayState))]
[JsonSerializable(typeof(PlayerCapabilities))]
[JsonSerializable(typeof(PlexServer.LibraryDirectoryType))]
[JsonSerializable(typeof(PlexServer.ServerNotificationType))]
[JsonSerializable(typeof(PlexServer.ClientsMediaContainer))]
[JsonSerializable(typeof(PlexServer.ClientServer))]
[JsonSerializable(typeof(PlexServer.ClientServer[]))]
[JsonSerializable(typeof(PlexServer.PlayStateNotification))]
[JsonSerializable(typeof(PlexServer.PlayStateNotification[]))]
[JsonSerializable(typeof(PlexServer.Response<PlexServer.ClientsMediaContainer>))]
[JsonSerializable(typeof(PlexServer.Response<PlexServer.MediaItemDetailContainer>))]
[JsonSerializable(typeof(PlexServer.Response<PlexServer.LibraryMediaContainer>))]
[JsonSerializable(typeof(PlexServer.ServerMessage))]
[JsonSerializable(typeof(PlexServer.ServerNotificationContainer))]
[JsonSerializable(typeof(PlexServer.MediaItemDetailContainer))]
[JsonSerializable(typeof(PlexServer.MediaItemMetadata))]
[JsonSerializable(typeof(PlexServer.MediaItemMetadata[]))]
[JsonSerializable(typeof(PlexServer.MediaPlayer))]
[JsonSerializable(typeof(PlexServer.LibraryDirectory))]
[JsonSerializable(typeof(PlexServer.LibraryDirectory[]))]
[JsonSerializable(typeof(PlexServer.LibraryMediaContainer))]
[JsonSerializable(typeof(JsonElement))]
[JsonSerializable(typeof(JsonElement?))]
internal sealed partial class PlexJsonSerializerContext : JsonSerializerContext;
