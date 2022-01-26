using System.Threading.Tasks;

namespace Neeo.Sdk.Devices.Controllers;

public interface IButtonController : IController
{
    ControllerType IController.Type => ControllerType.Button;

    Task TriggerAsync(string deviceId);
}

internal sealed class ButtonController : IButtonController
{
    private readonly string _button;
    private readonly ButtonHandler _buttonHandler;

    public ButtonController(ButtonHandler buttonHandler, string button) => (this._buttonHandler, this._button) = (buttonHandler, button);

    public Task TriggerAsync(string deviceId) => this._buttonHandler(deviceId, this._button);
}