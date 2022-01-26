using System;

namespace Neeo.Sdk.Notifications;



/// <summary>
/// Represents a notification to the Brain about a change in a component value for a device.
/// </summary>
/// <param name="UniqueDeviceId">The unique identifier of the device.</param>
/// <param name="Component">The component where the value has changed.</param>
/// <param name="Value">The updated value.</param>
public record struct Notification(String UniqueDeviceId, string Component, object Value);