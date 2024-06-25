using System.Text.Json.Serialization;

namespace Neeo.Drivers.Plex.ServerNotifications;

internal record struct Activity(
        [property: JsonPropertyName("cancellable")] bool Cancelable,
        int Progress,
        string Subtitle,
        string Title,
        string Type,
        [property: JsonPropertyName("userID")] int UserId,
        string Uuid
    );
