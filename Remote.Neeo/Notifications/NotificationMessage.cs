﻿namespace Remote.Neeo.Notifications;

internal record struct NotificationMessage(string Type, object Data)
{
    public const string DeviceSensorUpdateType = "DEVICE_SENSOR_UPDATE";
}