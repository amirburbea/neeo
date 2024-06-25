namespace Neeo.Drivers.Plex.ServerNotifications;

internal record struct StatusNotification(
    string Description,
    string NotificationName,
    string Title
);
