using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Features;

public interface IButtonFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Button;

    Task<SuccessResponse> TriggerAsync(string deviceId);
}

internal sealed class ButtonFeature : IButtonFeature
{
    private readonly string _button;
    private readonly ButtonHandler _buttonHandler;

    public ButtonFeature(ButtonHandler buttonHandler, string button) => (this._buttonHandler, this._button) = (buttonHandler, button);

    public async Task<SuccessResponse> TriggerAsync(string deviceId)
    {
        await this._buttonHandler(deviceId, this._button).ConfigureAwait(false);
        return true;
    }
}