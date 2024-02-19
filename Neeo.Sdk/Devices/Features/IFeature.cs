namespace Neeo.Sdk.Devices.Features;

/// <summary>
/// Describes a device feature.
///
/// Features are used by the integration server to fulfill requests or perform operations
/// on devices.
/// </summary>
public interface IFeature
{
    /// <summary>
    /// The type of the feature being modeled.
    /// </summary>
    FeatureType Type { get; }
}
