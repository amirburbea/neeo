using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Controllers;

public interface IDirectoryFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Directory;

    Task<SuccessResponse> PerformActionAsync(string deviceId, string actionIdentifier);
}

internal sealed class DirectoryFeature
{
}