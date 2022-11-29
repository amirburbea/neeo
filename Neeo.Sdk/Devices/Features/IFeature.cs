namespace Neeo.Sdk.Devices.Features;

/// <summary>
/// Describes a device feature.
/// </summary>
public interface IFeature
{
    /// <summary>
    /// The type of the feature being modeled.
    /// </summary>
    FeatureType Type { get; }
}

/// <summary>
/// Types of features for NEEO device drivers.
/// </summary>
public enum FeatureType
{
    /// <summary>
    /// Represents and handles associated callbacks related to buttons being pressed.
    /// </summary>
    Button,

    /// <summary>
    /// Represents and handles associated callbacks related to directories being browsed.
    /// </summary>
    Directory,

    /// <summary>
    /// Represents and handles associated callbacks related to device discovery.
    /// </summary>
    Discovery,

    /// <summary>
    /// Represents and handles associated callbacks related to custom favorites.
    /// </summary>
    Favorites,

    /// <summary>
    /// Represents and handles associated callbacks related to device registration.
    /// </summary>
    Registration,

    /// <summary>
    /// Represents and handles associated callbacks related to device subscription.
    /// </summary>
    Subscription,

    /// <summary>
    /// Represents and handles associated callbacks related to getting values from or setting values on a device.
    /// </summary>
    Value,
}