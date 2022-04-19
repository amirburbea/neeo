using System.Threading.Tasks;

namespace Neeo.Sdk.Devices;

/// <summary>
/// A callback which is invoked in response to a button being pressed on the NEEO remote
/// in order to allow the driver to respond accordingly.
/// </summary>
/// <param name="deviceId">The id associated with the device.</param>
/// <param name="buttonName">The name of the button being pressed.</param>
/// <returns><see cref="Task"/> to indicate completion.</returns>
/// <remarks>
/// Note:
/// <br />
/// a. <see cref="Button.TryResolve"/> may be able to translate the button name into a <see cref="Buttons"/> enumerated value.
/// <br />
/// b. <see cref="SmartApplicationButton.TryResolve"/> may be able to translate the button name into a <see cref="SmartApplicationButtons"/> enumerated value.
/// </remarks>
public delegate Task ButtonHandler(string deviceId, string buttonName);