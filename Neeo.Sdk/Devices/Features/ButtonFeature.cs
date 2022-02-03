using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Features;

public interface IButtonFeature : IFeature
{
    FeatureType IFeature.Type => FeatureType.Button;

    Task TriggerAsync(string deviceId);
}

internal sealed class ButtonFeature : IButtonFeature
{
    private readonly string _button;
    private readonly ButtonHandler _handler;

    public ButtonFeature(ButtonHandler handler, string button) => (this._handler, this._button) = (handler, button);

    public Task TriggerAsync(string deviceId) => this._handler.Invoke(deviceId, this._button);
}