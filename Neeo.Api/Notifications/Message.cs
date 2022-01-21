using System;
using System.Text.Json.Serialization;

namespace Neeo.Api.Notifications;

/// <summary>
/// Represents a notification to the Brain about a change in a component value for a device.
/// </summary>
public readonly struct Message
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Message"/> struct.
    /// </summary>
    /// <param name="uniqueDeviceId">The unique identifier of the device.</param>
    /// <param name="component">The component where the value has changed.</param>
    /// <param name="value">The updated value.</param>
    [JsonConstructor]
    public Message(string uniqueDeviceId, string component, object value)
    {
        this.UniqueDeviceId = uniqueDeviceId ?? throw new ArgumentNullException(nameof(uniqueDeviceId));
        this.Component = component ?? throw new ArgumentNullException(nameof(component));
        this.Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets the component where the value has changed.
    /// </summary>
    public string Component { get; }

    /// <summary>
    /// Gets the unique identifier of the device.
    /// </summary>
    public string UniqueDeviceId { get; }

    /// <summary>
    /// Gets the updated value.
    /// </summary>
    public object Value { get; }
}
