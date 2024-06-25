using System.Threading;
using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Features;

/// <summary>
/// Feature support for a single button.
/// </summary>
public interface IButtonFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Button;

    /// <summary>
    /// Executes the associated <see cref="ButtonHandler"/> when the button is pressed.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/> to represent the asynchronous operation.</returns>
    Task<SuccessResponse> ExecuteAsync(string deviceId, CancellationToken cancellationToken = default);
}

internal sealed class ButtonFeature(ButtonHandler handler, string button) : IButtonFeature
{
    public async Task<SuccessResponse> ExecuteAsync(string deviceId, CancellationToken cancellationToken)
    {
        await handler(deviceId, button, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
