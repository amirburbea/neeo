namespace Neeo.Sdk.Devices.Features;

public interface IFeature
{
    FeatureType Type { get; }
}

/// <summary>
/// 
/// </summary>
public enum FeatureType
{
    Button,
    Directory,
    Discovery,
    Favorites,
    Registration,
    Subscription,
    Value,
}