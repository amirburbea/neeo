using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Features;

/// <summary>
/// Feature support for a single button.
/// </summary>
public interface IButtonFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Button;

    /// <summary>
    /// The button has been pressed - execute the associated <see cref="ButtonHandler"/>.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <returns><see cref="Task"/> to represent the asynchronous operation.</returns>
    Task<SuccessResponse> ExecuteAsync(string deviceId);
}

internal sealed class ButtonFeature(ButtonHandler handler, string button) : IButtonFeature
{
    public async Task<SuccessResponse> ExecuteAsync(string deviceId)
    {
        await handler(deviceId, button).ConfigureAwait(false);
        return true;
    }
}
