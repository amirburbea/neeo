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

internal sealed class ButtonFeature : IButtonFeature
{
    private readonly string _button;
    private readonly ButtonHandler _handler;

    public ButtonFeature(ButtonHandler handler, string button) => (this._handler, this._button) = (handler, button);

    public async Task<SuccessResponse> ExecuteAsync(string deviceId)
    {
        await this._handler(deviceId, this._button).ConfigureAwait(false);
        return new(true);
    }
}