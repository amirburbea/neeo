namespace Neeo.Api.Notifications;

internal record struct Notification(string Type, object Data)
{
    public const string DeviceSensorUpdateType = "DEVICE_SENSOR_UPDATE";
}