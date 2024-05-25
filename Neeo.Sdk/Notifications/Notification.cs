namespace Neeo.Sdk.Notifications;

/// <summary>
/// Represents a notification to the Brain about a change in a component value for a device.
/// </summary>
/// <param name="DeviceId">The unique identifier of the device.</param>
/// <param name="Component">The component where the value has changed.</param>
/// <param name="Value">The updated value.</param>
public readonly record struct Notification(string DeviceId, string Component, object Value);
